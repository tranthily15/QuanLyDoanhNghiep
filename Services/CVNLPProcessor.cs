//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Linq;
//using System.Threading.Tasks;
//using Microsoft.ML;
//using Microsoft.ML.Data;
//using Microsoft.ML.Trainers;
//using Microsoft.ML.Transforms.Text;
//using iText.Kernel.Pdf;
//using iText.Kernel.Pdf.Canvas.Parser;
//using iText.Kernel.Pdf.Canvas.Parser.Listener;
//using QuanLyDoanhNghiep.Models;

//namespace QuanLyDoanhNghiep.Services
//{
//    public class CVNLPProcessor
//    {
//        private readonly MLContext _mlContext;
//        private ITransformer _model;
//        private readonly string _modelPath = "Models/cv_skills_model.zip";

//        public CVNLPProcessor()
//        {
//            _mlContext = new MLContext(seed: 0);
//            LoadOrTrainModel();
//        }

//        private void LoadOrTrainModel()
//        {
//            if (File.Exists(_modelPath))
//            {
//                _model = _mlContext.Model.Load(_modelPath, out var _);
//            }
//            else
//            {
//                TrainModel();
//            }
//        }

//        private void TrainModel()
//        {
//            var trainingData = new List<SkillData>();
//            var commonSkills = GetCommonSkills();
            
//            foreach (var skill in commonSkills)
//            {
//                trainingData.Add(new SkillData { Text = skill, Label = true });
//                // Thêm biến thể của kỹ năng
//                trainingData.Add(new SkillData { Text = $"Kinh nghiệm {skill}", Label = true });
//                trainingData.Add(new SkillData { Text = $"Thành thạo {skill}", Label = true });
//                trainingData.Add(new SkillData { Text = $"Có thể sử dụng {skill}", Label = true });
//                trainingData.Add(new SkillData { Text = $"Experience in {skill}", Label = true });
//                trainingData.Add(new SkillData { Text = $"Proficient in {skill}", Label = true });
//                trainingData.Add(new SkillData { Text = $"Skilled in {skill}", Label = true });
//            }

//            // Thêm dữ liệu negative
//            for (int i = 0; i < trainingData.Count / 2; i++)
//            {
//                trainingData.Add(new SkillData { Text = $"Không có kinh nghiệm về {commonSkills[i]}", Label = false });
//                trainingData.Add(new SkillData { Text = $"No experience in {commonSkills[i]}", Label = false });
//            }

//            var trainingDataView = _mlContext.Data.LoadFromEnumerable(trainingData);

//            var pipeline = _mlContext.Transforms.Text
//                .FeaturizeText(outputColumnName: "Features", inputColumnName: nameof(SkillData.Text))
//                .Append(_mlContext.BinaryClassification.Trainers.SdcaLogisticRegression());

//            _model = pipeline.Fit(trainingDataView);
//            _mlContext.Model.Save(_model, trainingDataView.Schema, _modelPath);
//        }

//        public async Task<List<ExtractedSkill>> ExtractSkillsFromCV(string cvFilePath)
//        {
//            var extractedSkills = new List<ExtractedSkill>();
//            var text = await ExtractTextFromPDF(cvFilePath);
//            var sentences = SplitIntoSentences(text);
//            var predictionEngine = _mlContext.Model.CreatePredictionEngine<SkillData, SkillPrediction>(_model);

//            foreach (var sentence in sentences)
//            {
//                var prediction = predictionEngine.Predict(new SkillData { Text = sentence });
//                if (prediction.Prediction && prediction.Probability > 0.7)
//                {
//                    var skillName = ExtractSkillName(sentence);
//                    if (!string.IsNullOrEmpty(skillName))
//                    {
//                        var skillLevel = EstimateSkillLevel(sentence);
//                        var confidence = prediction.Probability;
                        
//                        extractedSkills.Add(new ExtractedSkill
//                        {
//                            Name = skillName,
//                            Level = skillLevel,
//                            Confidence = confidence,
//                            Context = sentence,
//                            Category = DetermineSkillCategory(skillName)
//                        });
//                    }
//                }
//            }

//            return extractedSkills.GroupBy(s => s.Name)
//                                .Select(g => g.OrderByDescending(s => s.Confidence).First())
//                                .OrderByDescending(s => s.Confidence)
//                                .ToList();
//        }

//        private async Task<string> ExtractTextFromPDF(string filePath)
//        {
//            using (var pdfReader = new PdfReader(filePath))
//            using (var pdfDocument = new PdfDocument(pdfReader))
//            {
//                var text = new System.Text.StringBuilder();
//                for (int i = 1; i <= pdfDocument.GetNumberOfPages(); i++)
//                {
//                    var page = pdfDocument.GetPage(i);
//                    var strategy = new SimpleTextExtractionStrategy();
//                    var pageText = PdfTextExtractor.GetTextFromPage(page, strategy);
//                    text.Append(pageText);
//                }
//                return text.ToString();
//            }
//        }

//        private List<string> SplitIntoSentences(string text)
//        {
//            return text.Split(new[] { '.', '!', '?', '\n', ';', ',' }, StringSplitOptions.RemoveEmptyEntries)
//                      .Select(s => s.Trim())
//                      .Where(s => s.Length > 3)
//                      .ToList();
//        }

//        private string ExtractSkillName(string sentence)
//        {
//            var commonSkills = GetCommonSkills();
//            return commonSkills.FirstOrDefault(skill => 
//                sentence.Contains(skill, StringComparison.OrdinalIgnoreCase));
//        }

//        private int EstimateSkillLevel(string sentence)
//        {
//            var lowerSentence = sentence.ToLower();
            
//            if (lowerSentence.Contains("thành thạo") || 
//                lowerSentence.Contains("expert") || 
//                lowerSentence.Contains("chuyên gia") ||
//                lowerSentence.Contains("mastery"))
//                return 5;
            
//            if (lowerSentence.Contains("nâng cao") || 
//                lowerSentence.Contains("advanced") || 
//                lowerSentence.Contains("giỏi") ||
//                lowerSentence.Contains("proficient"))
//                return 4;
            
//            if (lowerSentence.Contains("kinh nghiệm") || 
//                lowerSentence.Contains("experienced") || 
//                lowerSentence.Contains("có thể") ||
//                lowerSentence.Contains("intermediate"))
//                return 3;
            
//            if (lowerSentence.Contains("biết") || 
//                lowerSentence.Contains("familiar") || 
//                lowerSentence.Contains("cơ bản") ||
//                lowerSentence.Contains("basic"))
//                return 2;
            
//            return 1;
//        }

//        private string DetermineSkillCategory(string skillName)
//        {
//            var lowerSkill = skillName.ToLower();

//            if (IsProgrammingLanguage(lowerSkill))
//                return "Programming Languages";

//            if (IsWebTechnology(lowerSkill))
//                return "Web Technologies";

//            if (IsDatabase(lowerSkill))
//                return "Databases";

//            if (IsDevOps(lowerSkill))
//                return "DevOps";

//            if (IsSoftSkill(lowerSkill))
//                return "Soft Skills";

//            if (IsDataScience(lowerSkill))
//                return "Data Science";

//            return "Other";
//        }

//        private bool IsProgrammingLanguage(string skill)
//        {
//            var programmingLanguages = new[] { "c#", "java", "python", "javascript", "typescript", "php", "ruby", "go", "rust", "swift", "kotlin" };
//            return programmingLanguages.Any(lang => skill.Contains(lang));
//        }

//        private bool IsWebTechnology(string skill)
//        {
//            var webTech = new[] { "html", "css", "react", "angular", "vue", "node.js", "asp.net", "spring", "django", "flask" };
//            return webTech.Any(tech => skill.Contains(tech));
//        }

//        private bool IsDatabase(string skill)
//        {
//            var databases = new[] { "sql", "mongodb", "postgresql", "mysql", "redis", "database" };
//            return databases.Any(db => skill.Contains(db));
//        }

//        private bool IsDevOps(string skill)
//        {
//            var devOps = new[] { "git", "docker", "aws", "azure", "kubernetes", "jenkins", "ci/cd", "devops" };
//            return devOps.Any(tool => skill.Contains(tool));
//        }

//        private bool IsSoftSkill(string skill)
//        {
//            var softSkills = new[] { "communication", "leadership", "problem solving", "team work", "project management" };
//            return softSkills.Any(soft => skill.Contains(soft));
//        }

//        private bool IsDataScience(string skill)
//        {
//            var dataScience = new[] { "machine learning", "ai", "data analysis", "statistics", "big data" };
//            return dataScience.Any(ds => skill.Contains(ds));
//        }

//        private List<string> GetCommonSkills()
//        {
//            return new List<string>
//            {
//                // Programming Languages
//                "C#", "Java", "Python", "JavaScript", "TypeScript", "PHP", "Ruby", "Go", "Rust", "Swift", "Kotlin",
//                "Lập trình C#", "Lập trình Java", "Lập trình Python", "Lập trình JavaScript", "Lập trình TypeScript",
//                "Lập trình PHP", "Lập trình Ruby", "Lập trình Go", "Lập trình Rust", "Lập trình Swift", "Lập trình Kotlin",

//                // Web Technologies
//                "HTML", "CSS", "React", "Angular", "Vue", "Node.js", "ASP.NET", "Spring", "Django", "Flask",
//                "Thiết kế web", "Phát triển frontend", "Phát triển backend", "Full-stack development",
//                "Web design", "Frontend development", "Backend development",

//                // Databases
//                "SQL", "MongoDB", "PostgreSQL", "MySQL", "Redis",
//                "Cơ sở dữ liệu", "Quản lý dữ liệu", "Database management",

//                // Cloud & DevOps
//                "Git", "Docker", "AWS", "Azure", "Kubernetes", "Terraform", "Jenkins", "CI/CD",
//                "Quản lý phiên bản", "Container hóa", "Điện toán đám mây", "DevOps",
//                "Version control", "Containerization", "Cloud computing",

//                // Soft Skills
//                "Communication", "Leadership", "Problem Solving", "Team Work", "Project Management",
//                "Giao tiếp", "Lãnh đạo", "Giải quyết vấn đề", "Làm việc nhóm", "Quản lý dự án",

//                // Data Science & AI
//                "Data Analysis", "Machine Learning", "AI", "Big Data", "IoT",
//                "Phân tích dữ liệu", "Học máy", "Trí tuệ nhân tạo", "Dữ liệu lớn", "Internet vạn vật",
//                "TensorFlow", "PyTorch", "Keras", "Scikit-learn", "Deep Learning",

//                // Mobile Development
//                "Android Development", "iOS Development", "React Native", "Flutter", "Xamarin",
//                "Phát triển ứng dụng di động", "Lập trình Android", "Lập trình iOS",

//                // Testing & QA
//                "Software Testing", "Quality Assurance", "Unit Testing", "Integration Testing",
//                "Kiểm thử phần mềm", "Đảm bảo chất lượng", "Selenium", "JUnit", "TestNG"
//            };
//        }
//    }

//    public class ExtractedSkill
//    {
//        public string Name { get; set; }
//        public int Level { get; set; }
//        public double Confidence { get; set; }
//        public string Context { get; set; }
//        public string Category { get; set; }

//        public string GetLevelText()
//        {
//            return Level switch
//            {
//                1 => "Cơ bản",
//                2 => "Trung bình",
//                3 => "Khá",
//                4 => "Giỏi",
//                5 => "Chuyên gia",
//                _ => "Không xác định"
//            };
//        }

//        public string GetLevelClass()
//        {
//            return Level switch
//            {
//                1 => "bg-secondary",
//                2 => "bg-info",
//                3 => "bg-primary",
//                4 => "bg-success",
//                5 => "bg-warning",
//                _ => "bg-secondary"
//            };
//        }
//    }

//    public class SkillData
//    {
//        public string Text { get; set; }
//        public bool Label { get; set; }
//    }

//    public class SkillPrediction
//    {
//        [VectorType(2)]
//        public float[] Score { get; set; }
//        public bool Prediction { get; set; }
//        public float Probability { get; set; }
//    }
//} 