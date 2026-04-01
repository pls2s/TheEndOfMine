namespace TheEndOfMine.Models;

public class SkillSet
{
    public int Survival { get; set; } = 1;   // ลด decay rate
    public int Combat { get; set; } = 1;      // ลด damage received
    public int Scavenging { get; set; } = 1;  // เพิ่มโอกาสเจอไอเทม
    public int Medicine { get; set; } = 1;    // เพิ่มประสิทธิภาพยา
}
