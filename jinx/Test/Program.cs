using System;
using System.Data;

class Program
{
    static void Main()
    {
        Console.WriteLine("简易计算器 (输入 'exit' 退出)");
        
        while (true)
        {
            Console.WriteLine("\n请输入计算公式");
            string formula = Console.ReadLine();
            
            if (formula?.ToLower() == "exit")
            {
                Console.WriteLine("感谢使用，再见！");
                break;
            }
            
            if (string.IsNullOrWhiteSpace(formula))
            {
                Console.WriteLine("输入不能为空，请重新输入");
                continue;
            }
            
            try
            {
                DataTable dt = new DataTable();
                object result = dt.Compute(formula, "");
                
                Console.WriteLine($"计算结果: {result}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"计算错误: {ex.Message}");
            }
        }
        
        Console.WriteLine("按任意键退出...");
        Console.ReadKey();
    }
}