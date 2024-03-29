﻿using PingPong.Applications;
using PingPong.KUKA;
using PingPong.OptiTrack;
using System;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Windows;

namespace PingPong {
    public partial class MainWindow : Window {

        private static MainWindow mainWindowHanlde;

        public MainWindow() {
            InitializeComponent();
            mainWindowHanlde = this;

            // Set timers resolution to 1ms (default resolution is 15.6ms)
            WinApi.TimeBeginPeriod(1);

            // Force change number separator to dot
            CultureInfo culuteInfo = new CultureInfo("en-US");
            culuteInfo.NumberFormat.NumberDecimalSeparator = ".";

            Thread.CurrentThread.CurrentCulture = culuteInfo;
            Thread.CurrentThread.CurrentUICulture = Thread.CurrentThread.CurrentCulture;

            Loaded += (s, e) => {
                robot1Panel.MainWindowHandle = this;
                robot1Panel.OptiTrack = optiTrackPanel.OptiTrack;
                robot2Panel.MainWindowHandle = this;
                robot2Panel.OptiTrack = optiTrackPanel.OptiTrack;

                try {
                    robot1Panel.LoadConfig(Path.Combine(App.ConfigDir, "robot1.config.json"));
                } catch (Exception) {
                    // Ignore exception
                }

                try {
                    robot2Panel.LoadConfig(Path.Combine(App.ConfigDir, "robot2.config.json"));
                } catch (Exception) {
                    // Ignore exception
                }

                Robot robot1 = robot1Panel.Robot;
                Robot robot2 = robot2Panel.Robot;
                OptiTrackSystem optiTrack = optiTrackPanel.OptiTrack;

                optiTrackPanel.Initialize(robot1, robot2);
                pingPanel.Initialize(robot1, robot2, optiTrack);

                robot1.ErrorOccured += (sender, args) => {
                    robot1Panel.ForceFreezeCharts();
                    robot2Panel.ForceFreezeCharts();
                    optiTrackPanel.ForceFreezeCharts();
                    pingPanel.ForceFreezeCharts();

                    ShowErrorDialog($"An exception was raised on the robot ({args.RobotIp}) thread.", args.Exception);
                };

                robot2.ErrorOccured += (sender, args) => {
                    robot1Panel.ForceFreezeCharts();
                    robot2Panel.ForceFreezeCharts();
                    optiTrackPanel.ForceFreezeCharts();
                    pingPanel.ForceFreezeCharts();

                    ShowErrorDialog($"An exception was raised on the robot ({args.RobotIp}) thread.", args.Exception);
                };

                pingPanel.Started += () => {
                    robot1Panel.DisableUIUpdates();
                    robot2Panel.DisableUIUpdates();
                    optiTrackPanel.DisableUIUpdates();
                };

                pingPanel.Stopped += () => {
                    robot1Panel.EnableUIUpdates();
                    robot2Panel.EnableUIUpdates();
                    optiTrackPanel.EnableUIUpdates();
                };
            };

            // Shrink window if it's too wide or too high
            double windowWidth = Width;
            double windowHeight = Height;

            double screenWidth = SystemParameters.PrimaryScreenWidth;
            double screenHeight = SystemParameters.PrimaryScreenHeight;

            if (windowWidth >= screenWidth) {
                Width = MinWidth = screenWidth - 100;
            }

            if (windowHeight >= screenHeight) {
                Height = MinHeight = screenHeight - 100;
            }

            // Robots configuration files directory
            Directory.CreateDirectory(App.ConfigDir);

            // Chart screenshots directory
            Directory.CreateDirectory(App.ScreenshotsDir);
        }

        public static void ShowErrorDialog(string errorMessage, Exception exception = null) {
            if (exception != null) {
                errorMessage += $"\nOriginal error: \"{exception.Message}\"";
            }

            if (mainWindowHanlde != null) {
                mainWindowHanlde.Dispatcher.Invoke(() => {
                    MessageBox.Show(mainWindowHanlde, errorMessage, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                });
            } else {
                MessageBox.Show(errorMessage, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

    }
}
