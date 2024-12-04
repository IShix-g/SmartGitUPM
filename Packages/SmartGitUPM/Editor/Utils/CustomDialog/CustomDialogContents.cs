
using System;

namespace SmartGitUPM.Editor
{
    public sealed class CustomDialogContents : IDisposable
    {
        public readonly string Title;
        public readonly string Message;
        public readonly string YesButtonText;
        public readonly string NoButtonText;
        public Action OnClickYes { get; private set; }
        public Action<bool> OnClose { get; private set; }

        bool _isDisposed;

        public CustomDialogContents(
            string title,
            string message,
            Action onClickYes,
            string yesButtonText = "",
            string noButtonText = "",
            Action<bool> onClose = default)
        {
            Title = title;
            Message = message;
            OnClickYes = onClickYes;
            YesButtonText = string.IsNullOrEmpty(yesButtonText) ? "Yes" : yesButtonText;
            NoButtonText = string.IsNullOrEmpty(noButtonText) ? "Close" : noButtonText;
            OnClose = onClose;
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;
            OnClickYes = default;
            OnClose = default;
        }
    }
}