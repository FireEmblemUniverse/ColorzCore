using System.Collections.Generic;
using ColorzCore.DataTypes;
using ColorzCore.IO;
using ColorzCore.Lexer;
using ColorzCore.Parser.AST;

namespace ColorzCore.Parser
{
    public class EAParseConsumer
    {
        public int CurrentOffset => currentOffset;

        public ImmutableStack<Closure> GlobalScope { get; }

        private readonly Stack<(int, bool)> pastOffsets; // currentOffset, offsetInitialized
        private readonly IList<(int, int, Location)> protectedRegions;

        private bool diagnosedOverflow; // true until first overflow diagnostic.
        private bool offsetInitialized; // false until first ORG, used for diagnostics
        private int currentOffset;

        Logger Logger { get; }

        public EAParseConsumer(Logger logger)
        {
            GlobalScope = new ImmutableStack<Closure>(new BaseClosure(), ImmutableStack<Closure>.Nil);
            pastOffsets = new Stack<(int, bool)>();
            protectedRegions = new List<(int, int, Location)>();
            currentOffset = 0;
            diagnosedOverflow = true;
            offsetInitialized = false;
            Logger = logger;
        }

        // TODO: these next two functions should probably be moved into their own module

        public static int ConvertToAddress(int value)
        {
            /*
                NOTE: Offset 0 is always converted to a null address
                If one wants to instead refer to ROM offset 0 they would want to use the address directly instead.
                If ROM offset 0 is already address 0 then this is a moot point.
            */

            if (value > 0 && value < EAOptions.MaximumBinarySize)
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

        // Helper method for statement handlers
        private int? EvaluteAtom(IAtomNode node)
        {
            return node.TryEvaluate(e => Logger.Error(node.MyLocation, e.Message), EvaluationPhase.Immediate);
        }

        public ILineNode? HandleRawStatement(RawNode node)
        {
            if ((CurrentOffset % node.Raw.Alignment) != 0)
            {
                Logger.Error(node.Location,
                    $"Bad alignment for raw {node.Raw.Name}: offset ({CurrentOffset:X8}) needs to be {node.Raw.Alignment}-aligned.");

                return null;
            }
            else
            {
                // TODO: more efficient spacewise to just have contiguous writing and not an offset with every line?
                CheckWriteBytes(node.Location, node.Size);
                return node;
            }
        }

        public ILineNode? HandleOrgStatement(Location location, IAtomNode offsetNode)
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

            return null;
        }

        public ILineNode? HandlePushStatement(Location _)
        {
            pastOffsets.Push((CurrentOffset, offsetInitialized));
            return null;
        }

        public ILineNode? HandlePopStatement(Location location)
        {
            if (pastOffsets.Count == 0)
            {
                Logger.Error(location, "POP without matching PUSH.");
            }
            else
            {
                (currentOffset, offsetInitialized) = pastOffsets.Pop();
            }

            return null;
        }

        public ILineNode? HandleAssertStatement(Location location, IAtomNode node)
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

            if (EvaluteAtom(node) is int result)
            {
                if (isBoolean && result == 0)
                {
                    Logger.Error(location, "Assertion failed");
                }
                else if (!isBoolean && result < 0)
                {
                    Logger.Error(location, $"Assertion failed with value {result}.");
                }
            }
            else
            {
                Logger.Error(node.MyLocation, "Failed to evaluate ASSERT expression.");
            }

            return null;
        }

        public ILineNode? HandleProtectStatement(Location location, IAtomNode beginAtom, IAtomNode? endAtom)
        {
            if (EvaluteAtom(beginAtom) is int beginValue)
            {
                beginValue = ConvertToAddress(beginValue);

                int length = 4;

                if (endAtom != null)
                {
                    if (EvaluteAtom(endAtom) is int endValue)
                    {
                        endValue = ConvertToAddress(endValue);

                        length = endValue - beginValue;

                        switch (length)
                        {
                            case < 0:
                                Logger.Error(location, $"Invalid PROTECT region: end address ({endValue:X8}) is before start address ({beginValue:X8}).");
                                return null;

                            case 0:
                                // NOTE: does this need to be an error?
                                Logger.Error(location, $"Empty PROTECT region: end address is equal to start address ({beginValue:X8}).");
                                return null;
                        }
                    }
                    else
                    {
                        // EvaluateAtom already printed an error message
                        return null;
                    }
                }

                protectedRegions.Add((beginValue, length, location));

                return null;
            }
            else
            {
                // EvaluateAtom already printed an error message
                return null;
            }
        }

        public ILineNode? HandleAlignStatement(Location location, IAtomNode alignNode, IAtomNode? offsetNode)
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
                                return null;
                            }
                        }
                        else
                        {
                            // EvaluateAtom already printed an error message
                            return null;
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

                    return null;
                }
                else
                {
                    Logger.Error(location, $"Invalid ALIGN value (got {alignValue}).");
                    return null;
                }
            }
            else
            {
                // EvaluateAtom already printed an error message
                return null;
            }
        }

        public ILineNode? HandleFillStatement(Location location, IAtomNode amountNode, IAtomNode? valueNode)
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
                            return null;
                        }
                    }

                    var data = new byte[amount];

                    for (int i = 0; i < amount; ++i)
                    {
                        data[i] = (byte)fillValue;
                    }

                    var node = new DataNode(CurrentOffset, data);

                    CheckWriteBytes(location, amount);
                    return node;
                }
                else
                {
                    Logger.Error(location, $"Invalid FILL amount (got {amount}).");
                    return null;
                }
            }
            else
            {
                // EvaluateAtom already printed an error message
                return null;
            }
        }

        public ILineNode? HandleSymbolAssignment(Location location, string name, IAtomNode atom, ImmutableStack<Closure> scopes)
        {
            if (atom.TryEvaluate(_ => { }, EvaluationPhase.Early) is int value)
            {
                TryDefineSymbol(location, scopes, name, value);
            }
            else
            {
                TryDefineSymbol(location, scopes, name, atom);
            }

            return null;
        }

        public ILineNode? HandleLabel(Location location, string name, ImmutableStack<Closure> scopes)
        {
            TryDefineSymbol(location, scopes, name, ConvertToAddress(CurrentOffset));
            return null;
        }

        public ILineNode? HandlePreprocessorLineNode(Location location, ILineNode node)
        {
            CheckWriteBytes(location, node.Size);
            return node;
        }

        public bool IsValidLabelName(string name)
        {
            // TODO: this could be where checks for CURRENTOFFSET, __LINE__ and __FILE__ are?
            return true; // !IsReservedName(name);
        }

        public void TryDefineSymbol(Location location, ImmutableStack<Closure> scopes, string name, int value)
        {
            if (scopes.Head.HasLocalSymbol(name))
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
                scopes.Head.AddSymbol(name, value);
            }
        }

        public void TryDefineSymbol(Location location, ImmutableStack<Closure> scopes, string name, IAtomNode expression)
        {
            if (scopes.Head.HasLocalSymbol(name))
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
                scopes.Head.AddSymbol(name, expression);
            }
        }

        // Return value: Location where protection occurred. Nothing if location was not protected.
        public Location? IsProtected(int offset, int length)
        {
            int address = ConvertToAddress(offset);

            foreach ((int protectedAddress, int protectedLength, Location location) in protectedRegions)
            {
                /* They intersect if the last offset in the given region is after the start of this one
                 * and the first offset in the given region is before the last of this one. */

                if (address + length > protectedAddress && address < protectedAddress + protectedLength)
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
