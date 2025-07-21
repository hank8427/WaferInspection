using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Shapes;
using GlueNet.VisionAI.Core.Models;
using GlueNet.VisionAI.Core.Operations;
using GlueNet.VisionAI.Recognitions.Aidi;
using Newtonsoft.Json;
using OpenCvSharp;
using Path = System.IO.Path;

namespace GlueNet.Vision.PTOT.Inspection
{
    public class AiDetector : INotifyPropertyChanged
    {
        private Stopwatch myStopwatch = new Stopwatch();

        private AidiRecognitionProject myAidiRecognitionProject;

        public string ProjectPath { get; set; }
        private int mySectionNumber { get; set; }
        private int myRowNumber { get; set; }
        private int myColumnNumber { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        public AiDetector(string projectPath)
        {
            ProjectPath = projectPath;
        }

        public async Task Initialize()
        {
            await CreateRecognitionPipeline(ProjectPath);
        }

        public void SetSize(int sectionNumber, int columnNumber, int rowNumber)
        {
            mySectionNumber = sectionNumber;
            myColumnNumber = columnNumber;
            myRowNumber = rowNumber;
        }

        public async Task<DyeResult> Run(string file)
        {
            myStopwatch.Restart();

            //var mat = new Mat(file);

            byte[] bytes = File.ReadAllBytes(file);

            var mat = Cv2.ImDecode(bytes, ImreadModes.Grayscale);

            myStopwatch.Stop();
            Console.WriteLine($@"Create Mat Elapsed Time: {myStopwatch.Elapsed.TotalMilliseconds} milliseconds");

            myStopwatch.Restart();

            var recognitionPipelineResult = await myAidiRecognitionProject.ExecuteAsync(mat.Clone(), new CancellationToken());

            myStopwatch.Stop();
            Console.WriteLine($@"Recognition Elapsed Time: {myStopwatch.Elapsed.TotalMilliseconds} milliseconds");

            int.TryParse(Path.GetFileNameWithoutExtension(file), out int index);

            var dyeDefectInfo = MergeOperationResult(recognitionPipelineResult.OperationResults)
                                .Where(x => (x.Rectangle.Width > 0 || x.Rectangle.Height > 0)).ToList();

            var dyeResult = new DyeResult
            {
                Name = Path.GetFileName(file),
                Row = index % myRowNumber,
                Column = index / myRowNumber % myColumnNumber,
                Section = index / myRowNumber / myColumnNumber,
                OKNG = dyeDefectInfo.Count == 0 ? "OK" : "NG",
                AiDetectResult = JsonConvert.SerializeObject(dyeDefectInfo),
            };

            return dyeResult;
        }

        private async Task CreateRecognitionPipeline(string projectPath)
        {
            myAidiRecognitionProject = new AidiRecognitionProject();
            myAidiRecognitionProject.ProjectPath = projectPath;
            await myAidiRecognitionProject.InitializeAsync();
        }

        private List<DyeDefect> MergeOperationResult(IReadOnlyList<IOperationResult> operationResultList)
        {
            var dyeDefectList = new List<DyeDefect>();

            var segmentations = operationResultList.Where(x => x.Type == OperationType.Segmentation);

            if (segmentations != null)
            {
                foreach (var segmentation in segmentations)
                {
                    foreach (var result in segmentation.GetResult() as IReadOnlyList<SegmentationData>)
                    {
                        var rect = new Rectangle(result.BoundingBox.X, result.BoundingBox.Y, result.BoundingBox.Width, result.BoundingBox.Height);

                        dyeDefectList.Add(new DyeDefect(result.Label, rect, result.Confidence, result.Points));
                    }
                }
            }
            return dyeDefectList;
        }
    }
}
