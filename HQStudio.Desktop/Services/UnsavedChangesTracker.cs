using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using HQStudio.Views.Dialogs;

namespace HQStudio.Services
{
    /// <summary>
    /// Интерфейс для отслеживания несохранённых изменений
    /// </summary>
    public interface IUnsavedChangesTracker
    {
        /// <summary>
        /// Есть ли несохранённые изменения
        /// </summary>
        bool HasUnsavedChanges { get; }

        /// <summary>
        /// Пометить как изменённое
        /// </summary>
        void MarkAsChanged();

        /// <summary>
        /// Пометить как сохранённое
        /// </summary>
        void MarkAsSaved();

        /// <summary>
        /// Сбросить состояние
        /// </summary>
        void Reset();

        /// <summary>
        /// Показать диалог подтверждения и вернуть true если пользователь подтвердил закрытие
        /// </summary>
        bool ConfirmDiscard(Window? owner = null);

        /// <summary>
        /// Событие изменения состояния
        /// </summary>
        event EventHandler<bool>? UnsavedChangesStateChanged;
    }

    /// <summary>
    /// Сервис для отслеживания несохранённых изменений в формах
    /// </summary>
    public class UnsavedChangesTracker : IUnsavedChangesTracker, INotifyPropertyChanged
    {
        private bool _hasUnsavedChanges;

        /// <summary>
        /// Есть ли несохранённые изменения
        /// </summary>
        public bool HasUnsavedChanges
        {
            get => _hasUnsavedChanges;
            private set
            {
                if (_hasUnsavedChanges != value)
                {
                    _hasUnsavedChanges = value;
                    OnPropertyChanged();
                    UnsavedChangesStateChanged?.Invoke(this, value);
                }
            }
        }

        /// <summary>
        /// Событие изменения состояния несохранённых изменений
        /// </summary>
        public event EventHandler<bool>? UnsavedChangesStateChanged;

        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Пометить форму как имеющую несохранённые изменения
        /// </summary>
        public void MarkAsChanged()
        {
            HasUnsavedChanges = true;
        }

        /// <summary>
        /// Пометить форму как сохранённую (без несохранённых изменений)
        /// </summary>
        public void MarkAsSaved()
        {
            HasUnsavedChanges = false;
        }

        /// <summary>
        /// Сбросить состояние отслеживания
        /// </summary>
        public void Reset()
        {
            HasUnsavedChanges = false;
        }

        /// <summary>
        /// Показать диалог подтверждения закрытия с несохранёнными изменениями
        /// </summary>
        /// <param name="owner">Родительское окно для диалога</param>
        /// <returns>true если пользователь подтвердил закрытие (отмену изменений), false если отменил</returns>
        public bool ConfirmDiscard(Window? owner = null)
        {
            if (!HasUnsavedChanges)
                return true;

            return ConfirmDialog.Show(
                "Несохранённые изменения",
                "У вас есть несохранённые изменения. Вы уверены, что хотите закрыть без сохранения?",
                ConfirmDialog.DialogType.Warning,
                "Закрыть без сохранения",
                "Отмена",
                owner);
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
