using System;
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

        public event PropertyChangedEventHandler PropertyChanged;

        public AiDetector(string projectPath)
        {
            ProjectPath = projectPath;
        }

        public async Task Initialize()
        {
            await CreateRecognitionPipeline(ProjectPath);
        }

        public async Task Run(string folderPath)
        {
            await DetectImage(folderPath);
        }

        private async Task CreateRecognitionPipeline(string projectPath)
        {
            myAidiRecognitionProject = new AidiRecognitionProject();
            myAidiRecognitionProject.ProjectPath = projectPath;
            await myAidiRecognitionProject.InitializeAsync();
        }

        private async Task DetectImage(string folder)
        {
            var files = Directory.GetFiles(folder, "*.bmp");

            foreach (var file in files)
            {
                var mat = new Mat(file);

                myStopwatch.Stop();
                Console.WriteLine($@"Create Mat Elapsed Time: {myStopwatch.Elapsed.TotalMilliseconds} milliseconds");

                myStopwatch.Restart();

                var recognitionPipelineResult = await myAidiRecognitionProject.ExecuteAsync(mat.Clone(), new CancellationToken());

                myStopwatch.Stop();
                Console.WriteLine($@"Recognition Elapsed Time: {myStopwatch.Elapsed.TotalMilliseconds} milliseconds");
            }
        }
    }
}
