using System;

namespace FtpTeste
{
    class Program
    {
        static void Main(string[] args)
        {
            var ftpOps = new FtpOperations();
            
            while (true)
            {
                Console.Write("escreva a SQL Query: ");
                string query = Console.ReadLine();
                ftpOps.Consultar(query);
                Console.WriteLine("Aperte Enter para limpar e voltar ao inicio");
                Console.ReadLine();
                Console.Clear();
            }
        }
    }
}
