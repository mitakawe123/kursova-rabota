﻿using corel_draw.Figures;
using corel_draw.Interfaces;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace corel_draw.Components
{
    internal class CommandManager
    {
        private List<ICommand> commandHistory = new List<ICommand>(); 
        private readonly List<ColorCommand> _colorCommandHistory = new List<ColorCommand>();
        private int commandIndex = -1;
        private ListBox actionList;

        public CommandManager(ListBox actionList)
        {
            this.actionList = actionList;
        }

        public void AddCommand(ICommand command)
        {
            commandHistory.RemoveRange(commandIndex + 1, commandHistory.Count - commandIndex - 1);
            commandHistory.Add(command);
            command.Do();
            commandIndex = commandHistory.Count - 1;
            actionList.Items.Add(command.ToString());
        }

        public void AddColorCommand(ColorCommand command)
        {
            _colorCommandHistory.RemoveRange(commandIndex + 1, _colorCommandHistory.Count - commandIndex - 1);
            _colorCommandHistory.Add(command);
            command.Do();
            commandIndex = _colorCommandHistory.Count - 1;
            actionList.Items.Add($"Undo: {command}");
        }

        public void Undo()
        {
            if (commandIndex >= 0)
            {
                ICommand command = commandHistory[commandIndex];
                command.Undo();
                commandIndex--; 
                actionList.Items.Add($"Undo: {command}");
            }
        }

        public void Redo()
        {
            if (commandIndex < commandHistory.Count - 1)
            {
                commandIndex++;
                ICommand command = commandHistory[commandIndex];
                command.Do();
                actionList.Items.Add($"Undo: {command}");
            }
        }

        public bool CanUndo()
        {
            return commandIndex >= 0;
        }

        public bool CanRedo()
        {
            return commandIndex < commandHistory.Count - 1;
        }
    }
}