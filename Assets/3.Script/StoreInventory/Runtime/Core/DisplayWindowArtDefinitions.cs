using System;
namespace GeneralStore.Inventory
{
    [Serializable]public sealed class DisplayWindowArtDatabase{public DisplayWindowArtDefinition[] entries;}
    [Serializable]public sealed class WindowArtRect{public float x,y,width,height;}
    [Serializable]public sealed class WindowArtSection{public int column,row,columns,rows;public WindowArtRect rect;}
    [Serializable]public sealed class DisplayWindowArtDefinition
    {
        public string typeId,spriteKey;public float sourceWidth,sourceHeight,displayHeight;
        public WindowArtSection[] sections;public WindowArtRect details,icon,text,close;
    }
    public static class DisplayWindowArtRules
    {
        static bool Positive(float value){return value>0&&!float.IsNaN(value)&&!float.IsInfinity(value);}
        static void Rect(WindowArtRect r,float width,float height)
        {if(r==null||!Positive(r.width)||!Positive(r.height)||float.IsNaN(r.x)||float.IsNaN(r.y)||r.x<0||r.y<0||r.x+r.width>width||r.y+r.height>height)throw new ArgumentException("창 Sprite 영역 좌표 오류입니다.");}
        public static void Validate(DisplayWindowArtDefinition art,DisplayTypeDefinition type)
        {
            if(art==null||type==null||art.typeId!=type.id||string.IsNullOrEmpty(art.spriteKey)||!Positive(art.sourceWidth)||!Positive(art.sourceHeight)||!Positive(art.displayHeight)||art.sections==null||art.sections.Length<1||art.sections.Length>8)throw new ArgumentException("창 Sprite 정의 오류입니다.");
            Rect(art.details,art.sourceWidth,art.sourceHeight);Rect(art.icon,art.sourceWidth,art.sourceHeight);Rect(art.text,art.sourceWidth,art.sourceHeight);Rect(art.close,art.sourceWidth,art.sourceHeight);
            var covered=new bool[type.columns*type.rows];
            foreach(var section in art.sections)
            {
                if(section==null||section.column<0||section.row<0||section.columns<1||section.rows<1||(long)section.column+section.columns>type.columns||(long)section.row+section.rows>type.rows)throw new ArgumentException("부분 격자 범위 오류입니다.");
                Rect(section.rect,art.sourceWidth,art.sourceHeight);
                for(int y=section.row;y<section.row+section.rows;y++)for(int x=section.column;x<section.column+section.columns;x++)
                {int key=y*type.columns+x;if(covered[key])throw new ArgumentException("부분 격자가 중복됩니다.");covered[key]=true;}
                // 부분 격자가 실제 모델 칸막이를 가로지르지 않게 합니다.
                if(!DisplayPartitionRules.Fits(type,new[]{new GridCell(0,0),new GridCell(section.columns-1,section.rows-1)},section.column,section.row))throw new ArgumentException("부분 격자가 모델 칸막이를 가로지릅니다.");
            }
            for(int a=0;a<art.sections.Length;a++)for(int b=a+1;b<art.sections.Length;b++)
            {var x=art.sections[a].rect;var y=art.sections[b].rect;if(x.x<y.x+y.width&&x.x+x.width>y.x&&x.y<y.y+y.height&&x.y+x.height>y.y)throw new ArgumentException("화면 격자 영역이 겹칩니다.");}
            foreach(bool value in covered)if(!value)throw new ArgumentException("화면에서 빠진 진열 칸이 있습니다.");
        }
    }
}
