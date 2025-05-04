//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Threading.Tasks;
//using Microsoft.EntityFrameworkCore;
//using Microsoft.ML;
//using Microsoft.ML.Data;
//using QuanLyDoanhNghiep.Models;
//using Microsoft.ML.Transforms.Text;
//using System.IO;

//namespace QuanLyDoanhNghiep.Services
//{
//    public class JobMatcher
//    {
//        private readonly QuanLyDoanhNghiepDBContext _context;
//        private readonly MLContext _mlContext;
//        private readonly TranslationService _translationService;
//        private ITransformer _model;
//        private readonly string _modelPath = Path.Combine(AppContext.BaseDirectory, "Models", "skill_vector_model.zip");
//        private PredictionEngine<SkillVectorData, SkillVectorPrediction> _predEngine;
//        private const int VectorDimension = 128;

//        public JobMatcher(QuanLyDoanhNghiepDBContext context, TranslationService translationService)
//        {
//            _context = context;
//            _translationService = translationService;
//            _mlContext = new MLContext(seed: 0);
//            LoadOrTrainModel();
//            if (_model != null)
//            {
//                _predEngine = _mlContext.Model.CreatePredictionEngine<SkillVectorData, SkillVectorPrediction>(_model);
//            }
//            else
//            {
//                Console.WriteLine("Error: Skill vector model could not be loaded or trained.");
//                _predEngine = null;
//            }
//        }

//        private void LoadOrTrainModel()
//        {
//            try
//            {
//                string modelDir = Path.GetDirectoryName(_modelPath);
//                if (!Directory.Exists(modelDir))
//                {
//                    Directory.CreateDirectory(modelDir);
//                    Console.WriteLine($"Created directory: {modelDir}");
//                }

//                if (File.Exists(_modelPath))
//                {
//                    Console.WriteLine($"Loading model from: {_modelPath}");
//                    _model = _mlContext.Model.Load(_modelPath, out var modelSchema);
//                    if (modelSchema == null || !modelSchema.TryGetColumnIndex("Features", out _) || modelSchema["Features"].Type.GetVectorSize() != VectorDimension)
//                    {
//                        Console.WriteLine($"Warning: Loaded model schema validation failed or dimension mismatch (Expected: {VectorDimension}). Attempting to retrain.");
//                        _model = null;
//                    }
//                    else
//                    {
//                        Console.WriteLine("Model loaded successfully.");
//                        return;
//                    }
//                }
//                else
//                {
//                    Console.WriteLine($"Model not found at {_modelPath}. Training new model...");
//                    var commonSkills = GetDefaultTrainingSkills();
//                    TrainModel(commonSkills);
//                    if (_model != null)
//                    {
//                        _mlContext.Model.Save(_model, null, _modelPath);
//                        Console.WriteLine($"New model trained and saved to: {_modelPath}");
//                    }
//                    else
//                    {
//                        Console.WriteLine("Error: Model training failed.");
//                    }
//                }
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"Error during model load/train: {ex.Message}");
//                _model = null;
//            }
//        }

//        private void TrainModel(IEnumerable<SkillVectorData> trainingData)
//        {
//            if (trainingData == null || !trainingData.Any())
//            {
//                Console.WriteLine("Training data is empty. Skipping model training.");
//                _model = null;
//                return;
//            }

//            try
//            {
//                var dataView = _mlContext.Data.LoadFromEnumerable(trainingData);
//                if (dataView == null)
//                {
//                    Console.WriteLine("Failed to load data view from training data.");
//                    _model = null;
//                    return;
//                }

//                var pipeline = _mlContext.Transforms.Text.FeaturizeText("Features_unnormalized", nameof(SkillVectorData.SkillName),
//                    options: new TextFeaturizingEstimator.Options()
//                    {
//                        WordFeatureExtractor = new WordBagEstimator.Options { NgramLength = 1, UseAllLengths = false },
//                        CharFeatureExtractor = new WordBagEstimator.Options { NgramLength = 3, UseAllLengths = false },
//                    })
//                    .Append(_mlContext.Transforms.NormalizeLpNorm("Features", "Features_unnormalized", norm: Microsoft.ML.Transforms.LpNormNormalizingEstimatorBase.NormFunction.L2));

//                Console.WriteLine("Fitting the model pipeline...");
//                _model = pipeline.Fit(dataView);
//                if (_model == null || !_model.GetOutputSchema(dataView.Schema).TryGetColumnIndex("Features", out _))
//                {
//                    Console.WriteLine("Model training finished, but the output schema does not contain the expected 'Features' column.");
//                    _model = null;
//                }
//                else
//                {
//                    Console.WriteLine("Model pipeline fitted successfully.");
//                }
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"Error during model training pipeline: {ex.Message}");
//                _model = null;
//            }
//        }

//        private float[] VectorizeSkill(string skillName)
//        {
//            if (_predEngine == null)
//            {
//                Console.WriteLine("Prediction engine is not available. Cannot vectorize skill.");
//                return null;
//            }
//            if (string.IsNullOrWhiteSpace(skillName)) return null;

//            try
//            {
//                var singleSkillData = new SkillVectorData { SkillName = skillName };
//                var prediction = _predEngine.Predict(singleSkillData);
//                if (prediction?.Features == null)
//                {
//                    return null;
//                }
//                if (prediction.Features.Length != VectorDimension)
//                {
//                    Console.WriteLine($"Warning: Vectorization for '{skillName}' produced vector of unexpected dimension {prediction.Features.Length} (Expected: {VectorDimension}).");
//                    return null;
//                }
//                return prediction.Features;
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"Error vectorizing skill '{skillName}': {ex.Message}");
//                return null;
//            }
//        }

//        private double CosineSimilarity(float[] vec1, float[] vec2)
//        {
//            if (vec1 == null || vec2 == null)
//            {
//                return 0.0;
//            }
//            if (vec1.Length != vec2.Length)
//            {
//                return 0.0;
//            }
//            if (vec1.Length == 0)
//            {
//                return 0.0;
//            }

//            double dotProduct = 0.0;
//            double norm1 = 0.0;
//            double norm2 = 0.0;
//            for (int i = 0; i < vec1.Length; i++)
//            {
//                dotProduct += vec1[i] * vec2[i];
//                norm1 += vec1[i] * vec1[i];
//                norm2 += vec2[i] * vec2[i];
//            }

//            if (norm1 == 0 || norm2 == 0)
//            {
//                return 0.0;
//            }

//            double similarity = dotProduct / (Math.Sqrt(norm1) * Math.Sqrt(norm2));

//            return Math.Max(-1.0, Math.Min(1.0, similarity));
//        }

//        public async Task<List<JobMatchResult>> FindMatchingJobsAsync(List<ExtractedSkill> extractedSkills)
//        {
//            if (_context == null)
//            {
//                Console.WriteLine("Database context is not available.");
//                return new List<JobMatchResult>();
//            }

//            if (extractedSkills == null || !extractedSkills.Any())
//            {
//                Console.WriteLine("No extracted skills provided.");
//                return new List<JobMatchResult>();
//            }

//            Console.WriteLine($"Received {extractedSkills.Count} skills to match.");

//            var candidateSkillVectors = extractedSkills
//                .Select(s => new { Skill = s, Vector = VectorizeSkill(s.Name) })
//                .Where(sv => sv.Vector != null && sv.Vector.Length > 0)
//                .ToList();

//            Console.WriteLine($"Successfully vectorized {candidateSkillVectors.Count} candidate skills.");

//            if (!candidateSkillVectors.Any())
//            {
//                Console.WriteLine("No candidate skills could be vectorized.");
//                return new List<JobMatchResult>();
//            }

//            List<JobPosition> jobPositions;
//            try
//            {
//                jobPositions = await _context.JobPositions
//                                             .Include(jp => jp.RequiredSkills)
//                                             .ThenInclude(rs => rs.Skill)
//                                             .Where(jp => jp.RequiredSkills.Any())
//                                             .ToListAsync();
//                Console.WriteLine($"Fetched {jobPositions.Count} job positions with requirements.");
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"Error fetching job positions from database: {ex.Message}");
//                return new List<JobMatchResult>();
//            }

//            var results = new List<JobMatchResult>();
//            const double similarityThreshold = 0.7;

//            foreach (var job in jobPositions)
//            {
//                if (job.RequiredSkills == null || !job.RequiredSkills.Any()) continue;

//                var jobSkillVectors = job.RequiredSkills
//                                        .Where(rs => rs.Skill != null && !string.IsNullOrWhiteSpace(rs.Skill.s_name))
//                                        .Select(rs => new { SkillName = rs.Skill.s_name, Vector = VectorizeSkill(rs.Skill.s_name) })
//                                        .Where(sv => sv.Vector != null && sv.Vector.Length > 0)
//                                        .ToList();

//                if (!jobSkillVectors.Any())
//                {
//                    continue;
//                }

//                var matchedSkills = new List<string>();
//                var missingSkills = new List<string>();
//                double totalSimilarityScore = 0;
//                int matchedCount = 0;

//                foreach (var jobSkillVec in jobSkillVectors)
//                {
//                    double maxSimilarityForJobSkill = 0;
//                    string bestMatchingCandidateSkill = null;

//                    foreach (var candidateSkillVec in candidateSkillVectors)
//                    {
//                        double similarity = CosineSimilarity(jobSkillVec.Vector, candidateSkillVec.Vector);
//                        if (similarity > maxSimilarityForJobSkill)
//                        {
//                            maxSimilarityForJobSkill = similarity;
//                            bestMatchingCandidateSkill = candidateSkillVec.Skill.Name;
//                        }
//                    }

//                    if (maxSimilarityForJobSkill >= similarityThreshold)
//                    {
//                        matchedSkills.Add(jobSkillVec.SkillName);
//                        totalSimilarityScore += maxSimilarityForJobSkill;
//                        matchedCount++;
//                    }
//                    else
//                    {
//                        missingSkills.Add(jobSkillVec.SkillName);
//                    }
//                }

//                if (matchedCount > 0)
//                {
//                    double matchPercentage = jobSkillVectors.Count > 0 ? ((double)matchedCount / jobSkillVectors.Count * 100) : 0.0;

//                    results.Add(new JobMatchResult
//                    {
//                        JobPositionId = job.jp_Id,
//                        CompanyName = job.jp_companyName ?? "N/A",
//                        PositionName = job.jp_name ?? "N/A",
//                        Location = job.jp_location ?? "N/A",
//                        Salary = job.jp_salary?.ToString("C0") ?? "Thương lượng",
//                        JobDescription = job.jp_description ?? "Không có mô tả",
//                        MatchPercentage = Math.Round(matchPercentage, 2),
//                        MatchedSkills = matchedSkills,
//                        MissingSkills = missingSkills
//                    });
//                }
//            }

//            var orderedResults = results.OrderByDescending(r => r.MatchPercentage).ToList();
//            Console.WriteLine($"Found {orderedResults.Count} matching jobs.");
//            return orderedResults;
//        }

//        private List<SkillVectorData> GetDefaultTrainingSkills()
//        {
//            return new List<SkillVectorData>
//            {
//                new SkillVectorData { SkillName = "C#" }, new SkillVectorData { SkillName = "Java" }, new SkillVectorData { SkillName = "Python" },
//                new SkillVectorData { SkillName = "JavaScript" }, new SkillVectorData { SkillName = "TypeScript" }, new SkillVectorData { SkillName = "HTML5" },
//                new SkillVectorData { SkillName = "CSS3" }, new SkillVectorData { SkillName = "SQL" }, new SkillVectorData { SkillName = "NoSQL" },
//                new SkillVectorData { SkillName = "React" }, new SkillVectorData { SkillName = "Angular" }, new SkillVectorData { SkillName = "Vue.js" },
//                new SkillVectorData { SkillName = "Node.js" }, new SkillVectorData { SkillName = "ASP.NET Core" }, new SkillVectorData { SkillName = ".NET Framework" },
//                new SkillVectorData { SkillName = "Spring Boot" }, new SkillVectorData { SkillName = "Django" }, new SkillVectorData { SkillName = "Flask" },
//                new SkillVectorData { SkillName = "Ruby on Rails" }, new SkillVectorData { SkillName = "PHP" }, new SkillVectorData { SkillName = "Laravel" },
//                new SkillVectorData { SkillName = "Swift" }, new SkillVectorData { SkillName = "Kotlin" }, new SkillVectorData { SkillName = "Objective-C" },
//                new SkillVectorData { SkillName = "Android Development" }, new SkillVectorData { SkillName = "iOS Development" }, new SkillVectorData { SkillName = "Mobile Development" },
//                new SkillVectorData { SkillName = "Flutter" }, new SkillVectorData { SkillName = "React Native" },
//                new SkillVectorData { SkillName = "Git" }, new SkillVectorData { SkillName = "Docker" }, new SkillVectorData { SkillName = "Kubernetes" },
//                new SkillVectorData { SkillName = "AWS" }, new SkillVectorData { SkillName = "Azure" }, new SkillVectorData { SkillName = "Google Cloud Platform" }, new SkillVectorData { SkillName = "Cloud Computing" },
//                new SkillVectorData { SkillName = "Linux" }, new SkillVectorData { SkillName = "Windows Server" },
//                new SkillVectorData { SkillName = "REST API" }, new SkillVectorData { SkillName = "GraphQL" },
//                new SkillVectorData { SkillName = "Microservices" }, new SkillVectorData { SkillName = "CI/CD" }, new SkillVectorData { SkillName = "Jenkins" }, new SkillVectorData { SkillName = "GitLab CI" },
//                new SkillVectorData { SkillName = "Terraform" }, new SkillVectorData { SkillName = "Ansible" },
//                new SkillVectorData { SkillName = "Machine Learning" }, new SkillVectorData { SkillName = "Deep Learning" }, new SkillVectorData { SkillName = "Data Science" },
//                new SkillVectorData { SkillName = "Data Analysis" }, new SkillVectorData { SkillName = "Natural Language Processing" }, new SkillVectorData { SkillName = "Computer Vision" },
//                new SkillVectorData { SkillName = "TensorFlow" }, new SkillVectorData { SkillName = "PyTorch" }, new SkillVectorData { SkillName = "Scikit-learn" },
//                new SkillVectorData { SkillName = "SQL Server" }, new SkillVectorData { SkillName = "MySQL" }, new SkillVectorData { SkillName = "PostgreSQL" }, new SkillVectorData { SkillName = "MongoDB" },
//                new SkillVectorData { SkillName = "Cybersecurity" }, new SkillVectorData { SkillName = "Network Security" },
//                new SkillVectorData { SkillName = "Project Management" }, new SkillVectorData { SkillName = "Agile Methodologies" }, new SkillVectorData { SkillName = "Scrum" }, new SkillVectorData { SkillName = "Kanban" },
//                new SkillVectorData { SkillName = "Communication" }, new SkillVectorData { SkillName = "Teamwork" }, new SkillVectorData { SkillName = "Collaboration" },
//                new SkillVectorData { SkillName = "Problem Solving" }, new SkillVectorData { SkillName = "Critical Thinking" }, new SkillVectorData { SkillName = "Analytical Skills" },
//                new SkillVectorData { SkillName = "Leadership" }, new SkillVectorData { SkillName = "Management" }, new SkillVectorData { SkillName = "Mentoring" },
//                new SkillVectorData { SkillName = "Adaptability" }, new SkillVectorData { SkillName = "Time Management" }, new SkillVectorData { SkillName = "Organization" },
//                new SkillVectorData { SkillName = "Creativity" }, new SkillVectorData { SkillName = "Innovation" },
//                new SkillVectorData { SkillName = "Customer Service" }, new SkillVectorData { SkillName = "Client Relationship Management" },
//                new SkillVectorData { SkillName = "Sales" }, new SkillVectorData { SkillName = "Marketing" }, new SkillVectorData { SkillName = "Digital Marketing" }, new SkillVectorData { SkillName = "SEO" },
//                new SkillVectorData { SkillName = "Business Analysis" }, new SkillVectorData { SkillName = "Requirements Gathering" },
//                new SkillVectorData { SkillName = "Financial Analysis" }, new SkillVectorData { SkillName = "Accounting" },
//                new SkillVectorData { SkillName = "Human Resources" }, new SkillVectorData { SkillName = "Recruitment" }, new SkillVectorData { SkillName = "Talent Acquisition" },
//                new SkillVectorData { SkillName = "UI/UX Design" }, new SkillVectorData { SkillName = "User Interface" }, new SkillVectorData { SkillName = "User Experience" },
//                new SkillVectorData { SkillName = "Testing" }, new SkillVectorData { SkillName = "Quality Assurance" }
//            };
//        }
//    }

//    public class SkillVectorData
//    {
//        [LoadColumn(0)]
//        public string SkillName { get; set; }
//    }

//    public class SkillVectorPrediction
//    {
//        [VectorType(128)]
//        public float[] Features { get; set; }
//    }

//    public class JobMatchResult
//    {
//        public int JobPositionId { get; set; }
//        public string CompanyName { get; set; }
//        public string PositionName { get; set; }
//        public string Location { get; set; }
//        public string Salary { get; set; }
//        public string JobDescription { get; set; }
//        public double MatchPercentage { get; set; }
//        public List<string> MatchedSkills { get; set; }
//        public List<string> MissingSkills { get; set; }
//    }

//    public class ExtractedSkill
//    {
//        public string Name { get; set; }
//        public int Level { get; set; }
//        public double Confidence { get; set; }
//        public string Context { get; set; }
//        public string Category { get; set; }
//    }
//} 