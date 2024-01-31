using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Taggu.Core;

public class DeepDanbooru
{
    private readonly InferenceSession _Session;
    private readonly List<string> _Tags;
    private readonly NodeMetadata _InputMetadata;

    public DeepDanbooru(string modelPath, string tagsPath, bool useDirectML = false)
    {
        var options = new SessionOptions();
        if (useDirectML)
        {
            options.AppendExecutionProvider_DML(0);
        }
        else
        {
            options.IntraOpNumThreads = 2;
            options.ExecutionMode = ExecutionMode.ORT_PARALLEL;
            options.InterOpNumThreads = 6;
            options.GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL;
            // options.OptimizedModelFilePath = "";
            options.AppendExecutionProvider_CPU(0);
        }

        _Session = new InferenceSession(modelPath, options);
        _InputMetadata = _Session.InputMetadata[_Session.InputNames[0]];
        _Tags = [.. File.ReadAllLines(tagsPath)];
    }

    public float[] LoadImage(string path)
    {
        var data = new float[_InputMetadata.Dimensions[1] * // width
                             _InputMetadata.Dimensions[2] * // height
                             _InputMetadata.Dimensions[3]]; // channel
        using (var image = Image.Load<Rgb24>(path))
        {
            image.Mutate(x => x.Resize(new ResizeOptions
            {
                Mode = ResizeMode.Pad,
                Size = new Size(_InputMetadata.Dimensions[1], _InputMetadata.Dimensions[2])
            }));

            var pixelCount = 0;
            for (var y = 0; y < image.Height; y++)
            {
                for (var x = 0; x < image.Width; x++)
                {
                    var pixel = image[x, y];
                    data[pixelCount++] = pixel.R / 255f;
                    data[pixelCount++] = pixel.G / 255f;
                    data[pixelCount++] = pixel.B / 255f;
                }
            }
        }
        return data;
    }

    public Dictionary<string, float> Evaluate(string path, float threshold = 0.5f)
    {
        var inputContainer = new List<NamedOnnxValue>();
        var input = LoadImage(path);
        var tensor = new DenseTensor<float>(input, 
            new int[] { 1, 
                _InputMetadata.Dimensions[1], 
                _InputMetadata.Dimensions[2], 
                _InputMetadata.Dimensions[3] 
            });
        inputContainer.Add(NamedOnnxValue.CreateFromTensor(_Session.InputNames[0], tensor));
        
        var results = _Session.Run(inputContainer);
        var output = results[0].AsTensor<float>().ToArray();
        var ret = new Dictionary<string, float>();
        for (var i = 0; i < output.Length; i++)
        {
            if (output[i] > threshold)
            {
                ret.Add(_Tags[i], output[i]);
            }
        }
        return ret;
    }
}
