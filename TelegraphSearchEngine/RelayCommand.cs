﻿using System;
using System.Windows.Input;

namespace TelegraphSearchEngine
{
    class RelayCommand(Action<object> execute, Func<object, bool> canExecute = null!) : ICommand
    {
        private readonly Action<object> execute = execute;
        private readonly Func<object, bool> canExecute = canExecute;


        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object? parameter)
        {
            return this.canExecute == null || this.canExecute(parameter ?? true);
        }
        public void Execute(object? parameter)
        {
            this.execute(parameter ?? true);
        }
    }
}

