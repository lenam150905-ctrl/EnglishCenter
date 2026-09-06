namespace EnglishCenter.API.DTOs
{
    public class ExcelImportResultDto
    {
        public int TotalRows { get; set; }

        public int SuccessRows { get; set; }

        public int FailedRows { get; set; }

        public List<string> Errors { get; set; } = new();
    }
}