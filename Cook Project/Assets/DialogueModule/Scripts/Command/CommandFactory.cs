using System;
using System.Collections.Generic;

namespace DialogueModule
{
    static class CommandFactory
    {
        private static readonly Dictionary<string, Func<GridInfo, StringGridRow, CommandBase>> _factoryMap = new();

        static CommandFactory() 
        {
            Register(CommandID.Character.ToStringFast(), (grid, row) => new CommandCharacter(grid, row));
            Register(CommandID.CharacterOff.ToStringFast(), (grid, row) => new CommandCharacterOff(grid, row));
            Register(CommandID.Text.ToStringFast(), (grid, row) => new CommandText(grid, row));

            Register(CommandID.Selection.ToStringFast(), (grid, row) => new CommandSelection(grid, row));
            Register(CommandID.SelectionEnd.ToStringFast(), (grid, row) => new CommandSelectionEnd());
            Register(CommandID.Jump.ToStringFast(), (grid, row) => new CommandJump(grid, row));
            Register(CommandID.EndScenario.ToStringFast(), (grid, row) => new CommandEndScenario(grid, row));
            Register(CommandID.ScenarioLabel.ToStringFast(), (grid, row) => new CommandScenarioLabel(grid, row));
        }

        public static void Register(string id, Func<GridInfo, StringGridRow, CommandBase> creator)
        {
            _factoryMap[id] = creator;
        }

        public static CommandBase Create(string id, GridInfo grid, StringGridRow row)
        {
            if (_factoryMap.TryGetValue(id, out var creator))
                return creator(grid, row);

            throw new ArgumentException($"Unknown command id: {id}");
        }
    }
}