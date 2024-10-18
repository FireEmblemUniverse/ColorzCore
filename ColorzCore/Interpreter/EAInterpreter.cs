using System.Collections.Generic;
using System.Linq;
using ColorzCore.DataTypes;
using ColorzCore.IO;
using ColorzCore.Lexer;
using ColorzCore.Parser;
using ColorzCore.Parser.AST;
using ColorzCore.Interpreter.Diagnostics;
using ColorzCore.Raws;

namespace ColorzCore.Interpreter
{
    public class EAInterpreter : IParseConsumer
    {
        public int CurrentOffset => currentOffset;

        public IList<Closure> AllScopes { get; }

        public ImmutableStack<Closure> GlobalScope { get; }
        public ImmutableStack<Closure> CurrentScope { get; set; }

        private readonly Stack<(int, bool)> pastOffsets; // currentOffset, offsetInitialized
        private readonly IList<(int, int, Location)> protectedRegions;

        private bool diagnosedOverflow; // false until first overflow diagnostic.
        private bool offsetInitialized; // false until first ORG, used for diagnostics
        private int currentOffset;

        public Logger Logger { get; }

        public EAInterpreter(Logger logger)
        {
            Closure headScope = new BaseClosure();
            AllScopes = new List<Closure>() { headScope };
            GlobalScope = new ImmutableStack<Closure>(headScope, ImmutableStack<Closure>.Nil);
            CurrentScope = GlobalScope;
            pastOffsets = new Stack<(int, bool)>();
            protectedRegions = new List<(int, int, Location)>();
            currentOffset = 0;
            diagnosedOverflow = false;
            offsetInitialized = false;
            Logger = logger;
        }

        // TODO: these next two functions should probably be moved into their own module

        public static int ConvertToAddress(int value)
        {
            if (value >= 0 && value < EAOptions.MaximumBinarySize)
            {
                value += EAOptions.BaseAddress;
            }

            return value;
        }

        public static int ConvertToOffset(int value)
        {
            if (value >= EAOptions.BaseAddress && value <= EAOptions.BaseAddress + EAOptions.MaximumBinarySize)
            {
                value -= EAOptions.BaseAddress;
            }

            return value;
        }

        public IAtomNode BindIdentifier(Token identifierToken)
        {
            return identifierToken.Content.ToUpperInvariant() switch
            {
                "CURRENTOFFSET" => new NumberNode(identifierToken, EAOptions.BaseAddress + CurrentOffset),
                _ => new IdentifierNode(identifierToken, CurrentScope),
            };
        }

        // Helper method for statement handlers
        private int? EvaluteAtom(IAtomNode node)
        {
            return node.TryEvaluate(e => Logger.Error(node.MyLocation, e.Message), EvaluationPhase.Immediate);
        }

        private IList<ILineNode> LineNodes { get; } = new List<ILineNode>();

        public IList<ILineNode> HandleEndOfInput()
        {
            if (CurrentScope != GlobalScope)
            {
                Logger.Error(null, "Reached end of input with an open local scope.");
            }

            return LineNodes;
        }

        #region AST Handlers

        public void OnOpenScope(Location _)
        {
            Closure newClosure = new Closure();

            AllScopes.Add(newClosure);
            CurrentScope = new ImmutableStack<Closure>(newClosure, CurrentScope);
        }

        public void OnCloseScope(Location location)
        {
            if (CurrentScope != GlobalScope)
            {
                CurrentScope = CurrentScope.Tail;
            }
            else
            {
                Logger.Error(location, "Attempted to close local scope without being within one.");
            }
        }

        public void OnRawStatement(Location location, Raw raw, IList<IParamNode> parameters)
        {
            RawNode node = new RawNode(raw, CurrentOffset, parameters);

            if ((CurrentOffset % node.Raw.Alignment) != 0)
            {
                Logger.Error(location,
                    $"Bad alignment for raw {node.Raw.Name}: offset ({CurrentOffset:X8}) needs to be {node.Raw.Alignment}-aligned.");
            }
            else
            {
                // TODO: more efficient spacewise to just have contiguous writing and not an offset with every line?
                CheckWriteBytes(location, node.Size);
                LineNodes.Add(node);
            }
        }

        public void OnOrgStatement(Location location, IAtomNode offsetNode)
        {
            if (EvaluteAtom(offsetNode) is int offset)
            {
                offset = ConvertToOffset(offset);

                if (!TrySetCurrentOffset(offset))
                {
                    Logger.Error(location, $"Invalid offset: 0x{offset:X}");
                }
                else
                {
                    diagnosedOverflow = false;
                    offsetInitialized = true;
                }
            }
            else
            {
                // EvaluateAtom already printed an error message
            }
        }

        public void OnPushStatement(Location _)
        {
            pastOffsets.Push((CurrentOffset, offsetInitialized));
        }

        public void OnPopStatement(Location location)
        {
            if (pastOffsets.Count == 0)
            {
                Logger.Error(location, "POP without matching PUSH.");
            }
            else
            {
                (currentOffset, offsetInitialized) = pastOffsets.Pop();
            }
        }

        public void OnAssertStatement(Location location, IAtomNode node)
        {
            // helper for distinguishing boolean expressions and other expressions
            // TODO: move elsewhere perhaps
            static bool IsBooleanResultHelper(IAtomNode node)
            {
                return node switch
                {
                    UnaryOperatorNode uon => uon.OperatorToken.Type switch
                    {
                        TokenType.LOGNOT_OP => true,
                        _ => false,
                    },

                    OperatorNode on => on.OperatorToken.Type switch
                    {
                        TokenType.LOGAND_OP => true,
                        TokenType.LOGOR_OP => true,
                        TokenType.COMPARE_EQ => true,
                        TokenType.COMPARE_NE => true,
                        TokenType.COMPARE_GT => true,
                        TokenType.COMPARE_GE => true,
                        TokenType.COMPARE_LE => true,
                        TokenType.COMPARE_LT => true,
                        _ => false,
                    },

                    _ => false,
                };
            }

            bool isBoolean = IsBooleanResultHelper(node);
            bool isSubtractionOfCurrentOffset = DiagnosticsHelpers.IsSubtractionOfCurrentOffset(node);

            if (EvaluteAtom(node) is int result)
            {
                if (isBoolean && result == 0)
                {
                    Logger.Error(location, "Assertion failed");
                }
                else if (!isBoolean && result < 0)
                {
                    /* users may do something like ASSERT UpperBound - CURRENTOFFSET
                     * If UpperBound is an offset rather than an address, this will now certainly fail.
                     * as CURRENTOFFSET was changed to be expanded into an address rather than a ROM offset.
                     * we do not want this to break too hard so we try to emit a warning rather than an error. */

                    if (isSubtractionOfCurrentOffset && result + EAOptions.BaseAddress >= 0)
                    {
                        Logger.Warning(location,
                            $"Assertion would fail with value {result} if CURRENTOFFSET is treated as an address.\n"
                          + "ColorzCore was recently changed to work in terms of addresses rather that ROM offsets.\n"
                          + "Consider changing the left operand to an address, or if you need to keep compatibility,\n"
                          + "You can also bitwise AND CURRENTOFFSET to keep it within the desired bounds.");
                    }
                    else
                    {
                        Logger.Error(location, $"Assertion failed with value {result}.");
                    }
                }
            }
            else
            {
                Logger.Error(node.MyLocation, "Failed to evaluate ASSERT expression.");
            }
        }

        public void OnProtectStatement(Location location, IAtomNode beginAtom, IAtomNode? endAtom)
        {
            if (EvaluteAtom(beginAtom) is int beginValue)
            {
                beginValue = ConvertToOffset(beginValue);

                int length = 4;

                if (endAtom != null)
                {
                    if (EvaluteAtom(endAtom) is int endValue)
                    {
                        endValue = ConvertToOffset(endValue);

                        length = endValue - beginValue;

                        switch (length)
                        {
                            case < 0:
                                Logger.Error(location, $"Invalid PROTECT region: end address ({endValue:X8}) is before start address ({beginValue:X8}).");
                                return;

                            case 0:
                                // NOTE: does this need to be an error?
                                Logger.Error(location, $"Empty PROTECT region: end address is equal to start address ({beginValue:X8}).");
                                return;
                        }
                    }
                    else
                    {
                        // EvaluateAtom already printed an error message
                        return;
                    }
                }

                protectedRegions.Add((beginValue, length, location));
            }
            else
            {
                // EvaluateAtom already printed an error message
            }
        }

        public void OnAlignStatement(Location location, IAtomNode alignNode, IAtomNode? offsetNode)
        {
            if (EvaluteAtom(alignNode) is int alignValue)
            {
                if (alignValue > 0)
                {
                    int alignOffset = 0;

                    if (offsetNode != null)
                    {
                        if (EvaluteAtom(offsetNode) is int rawOffset)
                        {
                            if (rawOffset >= 0)
                            {
                                alignOffset = ConvertToOffset(rawOffset) % alignValue;
                            }
                            else
                            {
                                Logger.Error(location, $"ALIGN offset cannot be negative (got {rawOffset})");
                                return;
                            }
                        }
                        else
                        {
                            // EvaluateAtom already printed an error message
                            return;
                        }
                    }

                    if (CurrentOffset % alignValue != alignOffset)
                    {
                        int skip = alignValue - (CurrentOffset + alignValue - alignOffset) % alignValue;

                        if (!TrySetCurrentOffset(CurrentOffset + skip) && !diagnosedOverflow)
                        {
                            Logger.Error(location, $"Trying to jump past end of binary (CURRENTOFFSET would surpass {EAOptions.MaximumBinarySize:X})");
                            diagnosedOverflow = true;
                        }
                    }
                }
                else
                {
                    Logger.Error(location, $"Invalid ALIGN value (got {alignValue}).");
                }
            }
            else
            {
                // EvaluateAtom already printed an error message
            }
        }

        public void OnFillStatement(Location location, IAtomNode amountNode, IAtomNode? valueNode)
        {
            if (EvaluteAtom(amountNode) is int amount)
            {
                if (amount > 0)
                {
                    int fillValue = 0;

                    if (valueNode != null)
                    {
                        if (EvaluteAtom(valueNode) is int rawValue)
                        {
                            fillValue = rawValue;
                        }
                        else
                        {
                            // EvaluateAtom already printed an error message
                            return;
                        }
                    }

                    var data = new byte[amount];

                    for (int i = 0; i < amount; ++i)
                    {
                        data[i] = (byte)fillValue;
                    }

                    var node = new DataNode(CurrentOffset, data);

                    CheckWriteBytes(location, amount);
                    LineNodes.Add(node);
                }
                else
                {
                    Logger.Error(location, $"Invalid FILL amount (got {amount}).");
                }
            }
            else
            {
                // EvaluateAtom already printed an error message
            }
        }

        public void OnSymbolAssignment(Location location, string name, IAtomNode atom)
        {
            if (atom.TryEvaluate(_ => { }, EvaluationPhase.Early) is int value)
            {
                TryDefineSymbol(location, name, value);
            }
            else
            {
                TryDefineSymbol(location, name, atom);
            }
        }

        public void OnLabel(Location location, string name)
        {
            TryDefineSymbol(location, name, ConvertToAddress(CurrentOffset));
        }

        public void OnData(Location location, byte[] data)
        {
            DataNode node = new DataNode(CurrentOffset, data);
            CheckWriteBytes(location, node.Size);
            LineNodes.Add(node);
        }

        #endregion

        public bool IsValidLabelName(string name)
        {
            // TODO: this could be where checks for CURRENTOFFSET, __LINE__ and __FILE__ are?
            return true; // !IsReservedName(name);
        }

        public void TryDefineSymbol(Location location, string name, int value)
        {
            if (CurrentScope.Head.HasLocalSymbol(name))
            {
                Logger.Warning(location, $"Symbol already in scope, ignoring: {name}");
            }
            else if (!IsValidLabelName(name))
            {
                // NOTE: IsValidLabelName returns true always. This is dead code
                Logger.Error(location, $"Invalid symbol name {name}.");
            }
            else
            {
                CurrentScope.Head.AddSymbol(name, value);
            }
        }

        public void TryDefineSymbol(Location location, string name, IAtomNode expression)
        {
            if (CurrentScope.Head.HasLocalSymbol(name))
            {
                Logger.Warning(location, $"Symbol already in scope, ignoring: {name}");
            }
            else if (!IsValidLabelName(name))
            {
                // NOTE: IsValidLabelName returns true always. This is dead code
                Logger.Error(location, $"Invalid symbol name {name}.");
            }
            else
            {
                CurrentScope.Head.AddSymbol(name, expression);
            }
        }

        // Return value: Location where protection occurred. Nothing if location was not protected.
        public Location? IsProtected(int offset, int length)
        {
            offset = ConvertToOffset(offset);

            foreach ((int protectedOffset, int protectedLength, Location location) in protectedRegions)
            {
                /* They intersect if the last offset in the given region is after the start of this one
                 * and the first offset in the given region is before the last of this one. */

                if (offset + length > protectedOffset && offset < protectedOffset + protectedLength)
                {
                    return location;
                }
            }

            return null;
        }

        public void CheckWriteBytes(Location location, int length)
        {
            if (!offsetInitialized && EAOptions.IsWarningEnabled(EAOptions.Warnings.UninitializedOffset))
            {
                Logger.Warning(location, "Writing before initializing offset. You may be breaking the ROM! (use `ORG offset` to set write offset).");
            }

            if (IsProtected(CurrentOffset, length) is Location prot)
            {
                Logger.Error(location, $"Trying to write data to area protected by {prot}");
            }

            if (!TrySetCurrentOffset(CurrentOffset + length) && !diagnosedOverflow)
            {
                Logger.Error(location, $"Trying to write past end of binary (CURRENTOFFSET would surpass {EAOptions.MaximumBinarySize:X})");
                diagnosedOverflow = true;
            }
        }

        private bool TrySetCurrentOffset(int value)
        {
            if (value < 0 || value > EAOptions.MaximumBinarySize)
            {
                return false;
            }
            else
            {
                currentOffset = value;
                diagnosedOverflow = false;
                offsetInitialized = true;
                return true;
            }
        }
    }
}
