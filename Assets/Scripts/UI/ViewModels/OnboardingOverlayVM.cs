using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Territory.UI.ViewModels
{
    /// <summary>
    /// Stage 5.0 (TECH-32924) — POCO ViewModel for onboarding-overlay modal.
    /// Exposes step progress + instruction text + next/skip commands.
    /// Implements INotifyPropertyChanged for UI Toolkit data binding.
    /// </summary>
    public sealed class OnboardingOverlayVM : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        // ── Commands ─────────────────────────────────────────────────────────
        public Action NextCommand { get; set; } = () => { };
        public Action SkipCommand { get; set; } = () => { };

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
