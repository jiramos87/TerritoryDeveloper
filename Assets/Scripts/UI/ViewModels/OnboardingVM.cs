using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Territory.UI.ViewModels
{
    /// <summary>
    /// Stage 5.0 (TECH-32925) — POCO ViewModel for onboarding fullscreen panel.
    /// Exposes welcome label + step progression + next/skip commands.
    /// Implements INotifyPropertyChanged for UI Toolkit data binding.
    /// </summary>
    public sealed class OnboardingVM : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        // ── Commands ─────────────────────────────────────────────────────────
        public Action NextCommand { get; set; } = () => { };
        public Action SkipCommand { get; set; } = () => { };

        // ── Welcome ───────────────────────────────────────────────────────────
        string _welcomeLabel = "Welcome to Territory";
        public string WelcomeLabel
        {
            get => _welcomeLabel;
            set { if (_welcomeLabel == value) return; _welcomeLabel = value; OnPropertyChanged(); }
        }

        // ── Step ──────────────────────────────────────────────────────────────
        int _currentStep = 1;
        int _totalSteps = 1;

        public int CurrentStep
        {
            get => _currentStep;
            set { if (_currentStep == value) return; _currentStep = value; OnPropertyChanged(); OnPropertyChanged(nameof(StepLabel)); }
        }

        public int TotalSteps
        {
            get => _totalSteps;
            set { if (_totalSteps == value) return; _totalSteps = value; OnPropertyChanged(); OnPropertyChanged(nameof(StepLabel)); }
        }

        public string StepLabel => $"STEP {_currentStep} OF {_totalSteps}";

        // ── Instruction ───────────────────────────────────────────────────────
        string _instruction = "";
        public string Instruction
        {
            get => _instruction;
            set { if (_instruction == value) return; _instruction = value; OnPropertyChanged(); }
        }

        void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
