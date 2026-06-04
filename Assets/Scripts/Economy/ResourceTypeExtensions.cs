namespace Jiangshi.Economy
{
    public static class ResourceTypeExtensions
    {
        public static string GetLabel(this ResourceType type)
        {
            return type switch
            {
                ResourceType.Gold => "金",
                ResourceType.Wood => "木",
                ResourceType.Food => "食",
                ResourceType.Power => "电",
                ResourceType.Population => "人口",
                ResourceType.Iron => "铁",
                ResourceType.Copper => "铜",
                _ => type.ToString()
            };
        }
    }
}
