using System;
using System.IO;
using System.Text;
namespace GeneralStore.Inventory
{
    // 원본 저장 파일에 직접 쓰지 않습니다. 임시 파일 완료 후 교체하고 직전 .bak을 남깁니다.
    public static class AtomicSaveFile
    {
        public static void Write(string path,string json)
        {
            string temp=path+"."+Guid.NewGuid().ToString("N")+".tmp";
            try
            {
                byte[] bytes=new UTF8Encoding(false).GetBytes(json);
                using(var stream=new FileStream(temp,FileMode.CreateNew,FileAccess.Write,FileShare.None))
                {stream.Write(bytes,0,bytes.Length);stream.Flush(true);}
                if(File.Exists(path)){if(Directory.Exists(path+".bak"))throw new IOException("백업 경로가 폴더입니다.");File.Copy(path,path+".bak",true);File.Replace(temp,path,null);}
                else File.Move(temp,path);
            }
            finally
            {try{if(File.Exists(temp))File.Delete(temp);}catch(IOException){/* 원래 실패 원인을 유지합니다. */}}
        }
    }
}
