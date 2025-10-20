using System.Collections.Generic;

namespace DialogueModule
{
    public class LabelData
    {
        public readonly string name;
        public readonly List<CommandBase> commands;

        public LabelData(string labelName, List<CommandBase> commands)
        {
            name = labelName;
            this.commands = commands;
        }
    }
}