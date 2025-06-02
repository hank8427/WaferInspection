using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GlueNet.VisionAI.Core.Models;
using GlueNet.VisionAI.Core.Operations;
using GlueNet.VisionAI.Recognitions.Aidi;
using Newtonsoft.Json;
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
            myStopwatch.Restart();

            //var mat = new Mat(file);

            byte[] bytes = File.ReadAllBytes(file);

            var mat = Cv2.ImDecode(bytes, ImreadModes.Color);

            myStopwatch.Stop();
            Console.WriteLine($@"Create Mat Elapsed Time: {myStopwatch.Elapsed.TotalMilliseconds} milliseconds");

            myStopwatch.Restart();

            var recognitionPipelineResult = await myAidiRecognitionProject.ExecuteAsync(mat.Clone(), new CancellationToken());

            myStopwatch.Stop();
            Console.WriteLine($@"Recognition Elapsed Time: {myStopwatch.Elapsed.TotalMilliseconds} milliseconds");

            int.TryParse(Path.GetFileNameWithoutExtension(file), out int index);

            var dyeDefectInfo = MergeOperationResult(recognitionPipelineResult.OperationResults)
                                .Where(x => (x.Rectangle.Width >= 100 || x.Rectangle.Height >= 100)).ToList();

            var dyeResult = new DyeResult
            {
                Name = Path.GetFileName(file),
                Row = index % RowNumber,
                Column = index / RowNumber,
                Section = index / RowNumber / ColumnNumber,
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
            if (operationResultList != null)
            {
                foreach (var operationResult in operationResultList)
                {
                    foreach (var result in operationResult.GetResult() as IReadOnlyList<SegmentationData>)
                    {
                        dyeDefectList.Add(new DyeDefect(result.Label, result.BoundingBox, result.Confidence));
                    }
                }
            }
            return dyeDefectList;
        }
    }
}
