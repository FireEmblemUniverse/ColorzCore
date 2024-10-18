using System;
using System.Collections.Generic;
using ColorzCore.DataTypes;
using ColorzCore.IO;
using ColorzCore.Parser;
using ColorzCore.Parser.AST;
using ColorzCore.Raws;

namespace ColorzCore.Interpreter.Diagnostics
{
    public class SetSymbolMacroDetector : IParseConsumer
    {
        public Logger Logger { get; }

        private enum State
        {
            AwaitingPush,
            AwaitingOrgAbsolute,
            AwaitingLabel,
            AwaitingPop,
            AwaitingEndOfMacro,
        }

        State suspectState = State.AwaitingPush;
        Location? suspectLocation = null;

        public SetSymbolMacroDetector(Logger logger)
        {
            Logger = logger;
            suspectState = State.AwaitingPush;
            suspectLocation = null;
        }

        private void OnSequenceBroken(Location location)
        {
            if (suspectState == State.AwaitingEndOfMacro && location.macroLocation != suspectLocation?.macroLocation)
            {
                Logger.Warning(suspectLocation,
                    "This looks like the expansion of a \"SetSymbol\" macro.\n"
                  + "SetSymbol macros are macros defined as `PUSH ; ORG value ; name : ; POP`.\n"
                  + "Because of changes to the behavior of labels, their use may introduce bugs.\n"
                  + "Consider using symbol assignment instead by writing `name := value`.");
            }

            suspectState = State.AwaitingPush;
            suspectLocation = null;
        }

        public void OnPushStatement(Location location)
        {
            OnSequenceBroken(location);

            suspectState = State.AwaitingOrgAbsolute;
            suspectLocation = location;
        }

        public void OnOrgStatement(Location location, IAtomNode offsetNode)
        {
            if (suspectState == State.AwaitingOrgAbsolute && location.macroLocation == suspectLocation?.macroLocation)
            {
                suspectState = State.AwaitingLabel;
            }
            else
            {
                OnSequenceBroken(location);
            }
        }

        public void OnLabel(Location location, string name)
        {
            if (suspectState == State.AwaitingLabel && location.macroLocation == suspectLocation?.macroLocation)
            {
                suspectState = State.AwaitingPop;
            }
            else
            {
                OnSequenceBroken(location);
            }
        }

        public void OnPopStatement(Location location)
        {
            if (suspectState == State.AwaitingPop && location.macroLocation == suspectLocation?.macroLocation)
            {
                suspectState = State.AwaitingEndOfMacro;
            }
            else
            {
                OnSequenceBroken(location);
            }
        }

        public void OnOpenScope(Location location) => OnSequenceBroken(location);

        public void OnCloseScope(Location location) => OnSequenceBroken(location);

        public void OnRawStatement(Location location, Raw raw, IList<IParamNode> parameters) => OnSequenceBroken(location);

        public void OnAssertStatement(Location location, IAtomNode node) => OnSequenceBroken(location);

        public void OnProtectStatement(Location location, IAtomNode beginAtom, IAtomNode? endAtom) => OnSequenceBroken(location);

        public void OnAlignStatement(Location location, IAtomNode alignNode, IAtomNode? offsetNode) => OnSequenceBroken(location);

        public void OnFillStatement(Location location, IAtomNode amountNode, IAtomNode? valueNode) => OnSequenceBroken(location);

        public void OnSymbolAssignment(Location location, string name, IAtomNode atom) => OnSequenceBroken(location);

        public void OnData(Location location, byte[] data) => OnSequenceBroken(location);
    }
}
