//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Threading.Tasks;
//using iText.Kernel.Pdf;
//using iText.Kernel.Pdf.Canvas.Parser;
//using iText.Kernel.Pdf.Canvas.Parser.Listener;
//using QuanLyDoanhNghiep.Models;

//namespace QuanLyDoanhNghiep.Services
//{
//    public class CVSkillExtractor
//    {
//        private readonly QuanLyDoanhNghiepDBContext _context;
//        private readonly List<string> _commonSkills;

//        public CVSkillExtractor(QuanLyDoanhNghiepDBContext context)
//        {
//            _context = context;
//            _commonSkills = new List<string>
//            {
//                "C#", "Java", "Python", "JavaScript", "HTML", "CSS", "SQL", "React", "Angular", "Vue",
//                "Node.js", "ASP.NET", "Spring", "Django", "Flask", "Git", "Docker", "AWS", "Azure",
//                "Linux", "Windows", "Agile", "Scrum", "JIRA", "Communication", "Leadership", "Problem Solving",
//                "Team Work", "Project Management", "Data Analysis", "Machine Learning", "AI", "DevOps",
//                "CI/CD", "Microservices", "REST API", "GraphQL", "MongoDB", "PostgreSQL", "MySQL",
//                "Redis", "Kubernetes", "Terraform", "Jenkins", "GitLab", "GitHub Actions"
//            };
//        }

//        public async Task<List<CVSkill>> ExtractSkillsFromCV(int cvId, string cvFilePath)
//        {
//           var extractedSkills = new List<CVSkill>();
//           var text = await ExtractTextFromPDF(cvFilePath);
//           var foundSkills = FindSkillsInText(text);

//           foreach (var skillName in foundSkills)
//           {
//               var skill = await GetOrCreateSkill(skillName);
//               var cvSkill = new CVSkill
//               {
//                   CVID = cvId,
//                   SkillID = skill.SkillID,
//                   SkillLevel = 3, // Default level, can be adjusted based on context
//                   Description = $"Extracted from CV"
//               };
//               extractedSkills.Add(cvSkill);
//           }

//           return extractedSkills;
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

//        private List<string> FindSkillsInText(string text)
//        {
//            var foundSkills = new List<string>();
//            foreach (var skill in _commonSkills)
//            {
//                if (text.Contains(skill, StringComparison.OrdinalIgnoreCase))
//                {
//                    foundSkills.Add(skill);
//                }
//            }
//            return foundSkills;
//        }

//        private async Task<Skill> GetOrCreateSkill(string skillName)
//        {
//           var skill = await _context.Skills.FirstOrDefaultAsync(s => s.SkillName == skillName);
//           if (skill == null)
//           {
//               skill = new Skill { SkillName = skillName };
//               _context.Skills.Add(skill);
//               await _context.SaveChangesAsync();
//           }
//           return skill;
//        }
//    }
//} 