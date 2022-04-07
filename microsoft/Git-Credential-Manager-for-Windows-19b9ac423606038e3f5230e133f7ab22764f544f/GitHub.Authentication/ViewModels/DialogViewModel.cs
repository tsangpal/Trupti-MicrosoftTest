﻿namespace GitHub.Authentication.ViewModels
{
    public class DialogViewModel : ViewModel
    {
        private AuthenticationDialogResult _result = AuthenticationDialogResult.None;
        public AuthenticationDialogResult Result
        {
            get { return _result; }
            protected set
            {
                _result = value;
                RaisePropertyChangedEvent(nameof(Result));
            }
        }

        private bool _isValid;
        public bool IsValid
        {
            get { return _isValid; }
            set
            {
                _isValid = value;
                RaisePropertyChangedEvent(nameof(IsValid));
            }
        }
    }
}
