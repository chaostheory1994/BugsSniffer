using BugsSniffer.Api;
using System.Threading.Tasks;

namespace BugsSniffer
{
    class Program
    {

        static async Task Main(string[] args)
        {
            await ProgramCode.Run();
        }
    }
}
