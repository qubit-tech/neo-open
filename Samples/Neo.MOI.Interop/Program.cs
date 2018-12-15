using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Neo.MOI.Interop
{
    class Program
    {
        public class RpcCommand
        {
            public string method { get; set; }
            public object[] @params { get; set; }
        }

        public class RpcCommandWithId : RpcCommand
        {
            public int id { get; set; }
        }

        static async Task<string> LoadSharedImage(HttpClient client, string rpcUri)
        {
            var imageName = "image-1";      // 共享图像名
            var fmt = "rgb24";              // 图像像素格式
            var w = 2048;                   // 图像宽度
            var h = 1024;                   // 图像高度
            var channels = 3;               // 图像颜色通道数
            var bytes = w * h * channels;   // 图像字节数

            using (var mapFile = System.IO.MemoryMappedFiles.MemoryMappedFile.CreateNew(imageName, bytes))
            using (var accessor = mapFile.CreateViewAccessor())
            {
                // 生成图像
                for (var y = 0; y < h; ++y)
                    for (var x = 0; x < w; ++x)
                    {
                        var i = ((y * w) + x) * 3;
                        var r = (x % w) * 256 / w;
                        var g = (y % h) * 256 / h;
                        accessor.Write(i + 0, (byte)r);
                        accessor.Write(i + 1, (byte)g);
                        accessor.Write(i + 2, (byte)0);
                    }

                var data = new byte[bytes];
                accessor.ReadArray(0, data, 0, data.Length);

                // 构建远程过程调用命令
                var cmds = new RpcCommand[]{
                    new RpcCommandWithId
                    {
                        id = 1,
                        method = "load_shared_image",
                        @params = new object[] { imageName, fmt, w, h },
                    },
                    new RpcCommandWithId
                    {
                        id = 2,
                        method = "inspect",
                        @params = new object[0],
                    },
                    new RpcCommand
                    {
                        method = "zoom",
                        @params = new object[]{ 0.5 },
                    },
                };

                // 远程过程调用
                var json = Newtonsoft.Json.JsonConvert.SerializeObject(cmds);
                var content = new StringContent(json, Encoding.UTF8, "application/json-rpc");
                var resp = await client.PostAsync(rpcUri, content);
                return await resp.Content.ReadAsStringAsync();
            }
        }
        static void Main(string[] args)
        {
            var client = new HttpClient();
            var rpcUri = "http://127.0.0.1:8080/rpc";
            var r = LoadSharedImage(client, rpcUri).Result;
            Console.WriteLine(r);
            Console.ReadLine();
        }
    }
}
