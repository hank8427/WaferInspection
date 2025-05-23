﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GlueNet.VisionAI.Recognitions.Aidi;
using OpenCvSharp;

namespace GlueNet.Vision.PTOT.WaferInspection
{
    public class AiDetector : INotifyPropertyChanged
    {
        private Stopwatch myStopwatch = new Stopwatch();

        private AidiRecognitionProject myAidiRecognitionProject;

        public string ProjectPath { get; set; }
        public int RowNumber { get; set; } = AppSettingsMgt.AppSettings.RowNumber;
        public int ColumnNumber { get; set; } = AppSettingsMgt.AppSettings.ColumnNumber;

        public event PropertyChangedEventHandler PropertyChanged;

        public AiDetector(string projectPath)
        {
            ProjectPath = projectPath;
        }

        public async Task Initialize()
        {
            await CreateRecognitionPipeline(ProjectPath);
        }

        public async Task<DyeResult> Run(string file)
        {
            var mat = new Mat(file);

            myStopwatch.Stop();
            Console.WriteLine($@"Create Mat Elapsed Time: {myStopwatch.Elapsed.TotalMilliseconds} milliseconds");

            myStopwatch.Restart();

            var recognitionPipelineResult = await myAidiRecognitionProject.ExecuteAsync(mat.Clone(), new CancellationToken());

            myStopwatch.Stop();
            Console.WriteLine($@"Recognition Elapsed Time: {myStopwatch.Elapsed.TotalMilliseconds} milliseconds");

            int.TryParse(Path.GetFileNameWithoutExtension(file), out int index);

            var dyeResult = new DyeResult
            {
                Name = Path.GetFileName(file),
                Row = index % RowNumber,
                Column = index / RowNumber,
                Section = index / RowNumber / ColumnNumber,
                AiDetectResult = recognitionPipelineResult.OperationResults.ToString()
            };

            return dyeResult;
        }

        private async Task CreateRecognitionPipeline(string projectPath)
        {
            myAidiRecognitionProject = new AidiRecognitionProject();
            myAidiRecognitionProject.ProjectPath = projectPath;
            await myAidiRecognitionProject.InitializeAsync();
        }
    }
}
