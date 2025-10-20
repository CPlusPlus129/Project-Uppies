using UnityEngine;

namespace DialogueModule
{
    class CommandEndScenario : CommandBase
    {
        public readonly string labelName;
        public CommandEndScenario(GridInfo grid, StringGridRow row) : base(CommandID.EndScenario, row)
        {
        }

        public override void Execute(DialogueEngine engine)
        {

        }
    }
}