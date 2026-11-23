using System;
using System.Collections.Generic;
namespace GeneralStore.Inventory
{
    [Serializable]public sealed class DisplayWindowArtDatabase{public DisplayWindowArtDefinition[] entries;}
    [Serializable]public sealed class WindowArtRect{public float x,y,width,height;}
    [Serializable]public sealed class WindowArtSection{public int column,row,columns,rows;public WindowArtRect rect;}
    [Serializable]public sealed class WindowArtFloor
    {public string spriteKey;public float sourceWidth,sourceHeight;public WindowArtRect art;public WindowArtSection[] sections;}
    [Serializable]public sealed class DisplayWindowArtDefinition
    {
        public string typeId,spriteKey;public float sourceWidth,sourceHeight,displayHeight;
        public WindowArtSection[] sections;public WindowArtRect details,icon,text,close;
        public bool scrolling;public WindowArtRect viewport,scrollbar;public WindowArtFloor[] floors;
    }
    public static class DisplayWindowArtRules
    {
        static bool Positive(float n){return n>0&&!float.IsNaN(n)&&!float.IsInfinity(n);}
        static void Rect(WindowArtRect r,float w,float h)
        {if(r==null||!Positive(r.width)||!Positive(r.height)||float.IsNaN(r.x)||float.IsNaN(r.y)||r.x<0||r.y<0||r.x+r.width>w||r.y+r.height>h)throw new ArgumentException("창 Sprite 영역 좌표 오류입니다.");}
        public static IEnumerable<WindowArtSection> Sections(DisplayWindowArtDefinition art)
        {
            if(art.scrolling){if(art.floors!=null)foreach(var floor in art.floors)if(floor?.sections!=null)foreach(var section in floor.sections)yield return section;}
            else if(art.sections!=null)foreach(var section in art.sections)yield return section;
        }
        static void ValidateSections(WindowArtSection[] sections,DisplayTypeDefinition type,float width,float height,bool[] covered,WindowArtRect crop=null)
        {
            if(sections==null||sections.Length<1||sections.Length>8)throw new ArgumentException("진열 부분 격자 누락");
            foreach(var section in sections)
            {
                if(section==null||section.column<0||section.row<0||section.columns<1||section.rows<1||section.column+section.columns>type.columns||section.row+section.rows>type.rows)throw new ArgumentException("부분 격자 범위 오류입니다.");
                Rect(section.rect,width,height);
                if(crop!=null&&(section.rect.x<crop.x||section.rect.y<crop.y||section.rect.x+section.rect.width>crop.x+crop.width||section.rect.y+section.rect.height>crop.y+crop.height))throw new ArgumentException("층 이미지 밖의 격자");
                for(int y=section.row;y<section.row+section.rows;y++)for(int x=section.column;x<section.column+section.columns;x++){int k=y*type.columns+x;if(covered[k])throw new ArgumentException("부분 격자 중복");covered[k]=true;}
                if(!DisplayPartitionRules.Fits(type,new[]{new GridCell(0,0),new GridCell(section.columns-1,section.rows-1)},section.column,section.row))throw new ArgumentException("부분 격자가 모델 칸막이를 가로지릅니다.");
            }
            for(int a=0;a<sections.Length;a++)for(int b=a+1;b<sections.Length;b++)
            {var x=sections[a].rect;var y=sections[b].rect;if(x.x<y.x+y.width&&x.x+x.width>y.x&&x.y<y.y+y.height&&x.y+x.height>y.y)throw new ArgumentException("화면 격자 영역 중복");}
        }
        public static void Validate(DisplayWindowArtDefinition art,DisplayTypeDefinition type)
        {
            if(art==null||type==null||art.typeId!=type.id||string.IsNullOrEmpty(art.spriteKey)||!Positive(art.sourceWidth)||!Positive(art.sourceHeight)||!Positive(art.displayHeight))throw new ArgumentException("창 Sprite 정의 오류입니다.");
            var covered=new bool[type.columns*type.rows];
            if(art.scrolling)
            {
                Rect(art.viewport,art.sourceWidth,art.sourceHeight);Rect(art.scrollbar,art.sourceWidth,art.sourceHeight);
                if(art.floors==null||art.floors.Length<1||art.floors.Length>16)throw new ArgumentException("스크롤 층수 오류");
                float contentHeight=0;
                foreach(var floor in art.floors)
                {
                    if(floor==null||string.IsNullOrEmpty(floor.spriteKey)||!Positive(floor.sourceWidth)||!Positive(floor.sourceHeight))throw new ArgumentException("층 Sprite 누락");
                    Rect(floor.art,floor.sourceWidth,floor.sourceHeight);ValidateSections(floor.sections,type,floor.sourceWidth,floor.sourceHeight,covered,floor.art);contentHeight+=floor.art.height;
                }
                if(contentHeight<=art.viewport.height)throw new ArgumentException("스크롤할 추가 층이 없습니다.");
            }
            else ValidateSections(art.sections,type,art.sourceWidth,art.sourceHeight,covered);
            foreach(bool present in covered)if(!present)throw new ArgumentException("화면에서 빠진 진열 칸이 있습니다.");
        }
    }
}
