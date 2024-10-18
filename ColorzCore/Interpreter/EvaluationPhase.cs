using System;

namespace ColorzCore.Interpreter
{
    public enum EvaluationPhase
    {
        // Early-evaluation: we are not done with parsing and obtaining a value now would just be an optimization
        Early,

        // Final-evaluation: we are done with parsing and need to freeze this value now
        Final,

        // Immediate-evaluation: we are not done with parsing but need this value now
        Immediate,
    }
}
