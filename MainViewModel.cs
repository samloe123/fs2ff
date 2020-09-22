﻿using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Media;
using fs2ff.FlightSim;
using fs2ff.ForeFlight;
using fs2ff.Models;

#pragma warning disable 67

// ReSharper disable UnusedMember.Local

namespace fs2ff
{
    public class MainViewModel : INotifyPropertyChanged, IFlightSimMessageHandler
    {
        private readonly FlightSimService _flightSim;
        private readonly ForeFlightService _foreFlight;

        private bool _errorOccurred;
        private IntPtr _hwnd = IntPtr.Zero;

        public MainViewModel(ForeFlightService foreFlight, FlightSimService flightSim)
        {
            _foreFlight = foreFlight;
            _flightSim = flightSim;
            _flightSim.StateChanged += FlightSim_StateChanged;
            _flightSim.PositionReceived += FlightSim_PositionReceived;
            _flightSim.AttitudeReceived += FlightSim_AttitudeReceived;
            _flightSim.TrafficReceived += FlightSim_TrafficReceived;

            ToggleConnectCommand = new ActionCommand(ToggleConnect, CanConnect);

            UpdateState();
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private enum FlightSimState
        {
            Unknown,
            Connected,
            Disconnected,
            ErrorOccurred
        }

        public string? ConnectButtonLabel { get; set; }

        public Brush? StateLabelColor { get; set; }

        public string? StateLabelText { get; set; }

        public ActionCommand ToggleConnectCommand { get; }

        private bool Connected => _flightSim.Connected;

        private FlightSimState CurrentFlightSimState =>
            _errorOccurred
                ? FlightSimState.ErrorOccurred
                : Connected
                    ? FlightSimState.Connected
                    : FlightSimState.Disconnected;

        public IntPtr WindowHandle
        {
            get => _hwnd;
            set
            {
                _hwnd = value;
                ToggleConnectCommand.TriggerCanExecuteChanged();
            }
        }

        public static string WindowTitle => $"fs2ff (Flight Simulator -> ForeFlight) {App.AssemblyVersion}";

        public void Dispose()
        {
            _flightSim.TrafficReceived -= FlightSim_TrafficReceived;
            _flightSim.AttitudeReceived -= FlightSim_AttitudeReceived;
            _flightSim.PositionReceived -= FlightSim_PositionReceived;
            _flightSim.StateChanged -= FlightSim_StateChanged;
            _flightSim.Dispose();
            _foreFlight.Dispose();
        }

        public void ReceiveFlightSimMessage() => _flightSim.ReceiveMessage();

        private bool CanConnect() => WindowHandle != IntPtr.Zero;

        private void Connect() => _flightSim.Connect(WindowHandle);

        private void Disconnect() => _flightSim.Disconnect();

        private async Task FlightSim_AttitudeReceived(Attitude att)
        {
            await _foreFlight.Send(att).ConfigureAwait(false);
        }

        private async Task FlightSim_PositionReceived(Position pos)
        {
            await _foreFlight.Send(pos).ConfigureAwait(false);
        }

        private void FlightSim_StateChanged(bool failure)
        {
            _errorOccurred = failure;
            UpdateState();
        }

        private async Task FlightSim_TrafficReceived(Traffic tfk, uint id)
        {
            await _foreFlight.Send(tfk, id).ConfigureAwait(false);
        }

        private void ToggleConnect()
        {
            if (Connected) Disconnect();
            else              Connect();
        }

        private void UpdateState()
        {
            (ConnectButtonLabel, StateLabelColor, StateLabelText) = CurrentFlightSimState switch
            {
                FlightSimState.Connected      => ("Disconnect", Brushes.Goldenrod, "CONNECTED"),
                FlightSimState.Disconnected   => ("Connect", Brushes.DarkGray, "NOT CONNECTED"),
                FlightSimState.ErrorOccurred  => ("Connect", Brushes.OrangeRed, "UNABLE TO CONNECT"),
                _                             => ("Connect", Brushes.DarkGray, "")
            };
        }
    }
}
