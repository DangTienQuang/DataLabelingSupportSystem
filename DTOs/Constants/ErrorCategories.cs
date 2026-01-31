namespace DTOs.Constants
{
    public static class ErrorCategories
    {
        public const string LU01_IncorrectLabel = "LU-01: Incorrect label definition";
        public const string LU02_Confusion = "LU-02: Label confusion";

        public const string TE01_WrongBox = "TE-01: Incorrect labeling region";
        public const string TE02_LooseBox = "TE-02: Bounding box too loose";
        public const string TE03_TightBox = "TE-03: Bounding box too tight";
        public const string TE04_OcclusionError = "TE-04: Incorrect handling of occlusion";

        public const string ME01_MissingObject = "ME-01: Missing object";
        public const string ME02_ExtraLabel = "ME-02: Extra / redundant label";

        public const string PR01_ProcessError = "PR-01: Process error";
        public const string Other = "Other: Other errors";

        public static readonly List<string> All = new()
        {
            LU01_IncorrectLabel, LU02_Confusion,
            TE01_WrongBox, TE02_LooseBox, TE03_TightBox, TE04_OcclusionError,
            ME01_MissingObject, ME02_ExtraLabel,
            PR01_ProcessError, Other
        };

        public static bool IsValid(string category) => All.Contains(category);

        public static int GetSeverityWeight(string category)
        {
            if (category.StartsWith("LU-01") || category.StartsWith("ME-01") ||
                category.StartsWith("TE-01") || category.StartsWith("PR-01"))
                return 10;

            if (category.StartsWith("TE-02") || category.StartsWith("TE-03") ||
                category.StartsWith("TE-04") || category.StartsWith("LU-02"))
                return 5;

            return 2;
        }
    }
}