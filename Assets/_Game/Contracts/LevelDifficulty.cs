namespace WordStack.Contracts
{
    /// <summary>
    /// Độ khó của một màn, khai báo trong file level (`"difficulty"` trong JSON).
    ///
    /// Sống ở Contracts vì cả hai phía đều cần: gameplay đọc từ JSON, meta dùng để
    /// chọn sprite khung level. Contracts là ranh giới duy nhất hai bên cùng thấy.
    /// </summary>
    public enum LevelDifficulty
    {
        Normal = 0,
        Hard = 1,
        Crazy = 2,
    }
}
