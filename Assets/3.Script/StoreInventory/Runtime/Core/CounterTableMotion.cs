using System;
using System.Collections.Generic;
namespace GeneralStore.Inventory
{
    // Display-only bodies. They never own inventory, change UIDs, or create floor stock.
    public sealed class CounterBodyState
    {
        public float x,y,vx,vy,halfWidth,halfHeight;
        public bool active,dragged,asleep;
        public float Bottom{get{return y-halfHeight;}}
        public float Top{get{return y+halfHeight;}}
    }
    public sealed class CounterTableSettings
    {
        public float left=100,right=1820,surface=-1028.5f,ceiling=-40;
        public float gravity=2200,bounce=.1f,drag=6,sleepSpeed=25;
        static bool Finite(float n){return !float.IsNaN(n)&&!float.IsInfinity(n);}
        public void Validate()
        {
            if(!Finite(left)||!Finite(right)||!Finite(surface)||!Finite(ceiling)||!Finite(gravity)||!Finite(bounce)||!Finite(drag)||!Finite(sleepSpeed)
                ||right<=left||ceiling<=surface||gravity<=0||gravity>20000||bounce<0||bounce>.5f||drag<0||sleepSpeed<=0)
                throw new ArgumentException("Counter surface/gravity settings are invalid.");
        }
    }
    public static class CounterTableMotion
    {
        static float Clamp(float n,float lo,float hi){return Math.Max(lo,Math.Min(hi,n));}
        public static WindowPoint VisualSize(int columns,int rows,float cell,float aspect)
        {
            if(columns<1||rows<1||cell<=0||float.IsNaN(cell)||float.IsInfinity(cell))throw new ArgumentException("Counter cell size is invalid.");
            float width=columns*cell,height=rows*cell;
            if(aspect>0&&!float.IsNaN(aspect)&&!float.IsInfinity(aspect))
            {if(width/height>aspect)width=height*aspect;else height=width/aspect;}
            return new WindowPoint(width,height);
        }
        public static void ClampBody(CounterBodyState body,CounterTableSettings settings)
        {
            if(float.IsNaN(body.x)||float.IsInfinity(body.x)||float.IsNaN(body.y)||float.IsInfinity(body.y)||float.IsNaN(body.halfWidth)||float.IsInfinity(body.halfWidth)||float.IsNaN(body.halfHeight)||float.IsInfinity(body.halfHeight)||body.halfWidth<=0||body.halfHeight<=0||body.halfWidth*2>settings.right-settings.left||body.halfHeight*2>settings.ceiling-settings.surface)
                throw new ArgumentException("Counter item does not fit the configured table bounds.");
            body.x=Clamp(body.x,settings.left+body.halfWidth,settings.right-body.halfWidth);
            body.y=Clamp(body.y,settings.surface+body.halfHeight,settings.ceiling-body.halfHeight);
        }
        // Presentation only. Never write this rotation back to ItemRecord.
        public static int PresentationRotation(int rows,bool customerPicked,int sourceRotation)
        {if(rows<1||sourceRotation<0||sourceRotation>3)throw new ArgumentException("Counter rotation is invalid.");return rows>=3?(sourceRotation+1)%4:sourceRotation;}
        public static float SurfaceDepth(float authoredDepth,float height,float lowerPercent,float bottomMargin)
        {
            if(float.IsNaN(authoredDepth)||float.IsInfinity(authoredDepth)||float.IsNaN(height)||float.IsInfinity(height)||float.IsNaN(lowerPercent)||float.IsInfinity(lowerPercent)||float.IsNaN(bottomMargin)||float.IsInfinity(bottomMargin)||authoredDepth<=0||height<=bottomMargin||bottomMargin<0||lowerPercent<0||lowerPercent>100)
                throw new ArgumentException("Counter surface depth is invalid.");
            return Math.Min(height-bottomMargin,authoredDepth*(1+lowerPercent*.01f));
        }
        public static int Advance(IList<CounterBodyState> bodies,CounterTableSettings settings,float seconds)
        {
            settings.Validate();
            if(seconds<0||seconds>.1f||float.IsNaN(seconds)||float.IsInfinity(seconds))throw new ArgumentException("Counter delta time is invalid.");
            bool moving=false;foreach(var b in bodies)if(b.active&&!b.dragged&&!b.asleep){moving=true;break;}
            if(!moving||seconds==0)return 0;
            int steps=Math.Max(1,(int)Math.Ceiling(seconds*120)),work=0;float dt=seconds/steps;
            for(int step=0;step<steps;step++)foreach(var body in bodies)
            {
                if(!body.active||body.dragged)continue;
                
                if(body.asleep)
                {if(Math.Abs(body.Bottom-settings.surface)<.5f)continue;body.asleep=false;}
                work++;body.vx*=(float)Math.Exp(-settings.drag*dt);body.vy-=settings.gravity*dt;
                body.x+=body.vx*dt;body.y+=body.vy*dt;
                float left=settings.left+body.halfWidth,right=settings.right-body.halfWidth;
                if(body.x<left||body.x>right){body.x=Clamp(body.x,left,right);body.vx=-body.vx*.15f;}
                float support=settings.surface; // Goods never support other goods.
                if(body.Bottom<=support&&body.vy<=0)
                {
                    body.y=support+body.halfHeight;float speed=-body.vy*settings.bounce;
                    body.vy=speed<=settings.sleepSpeed?0:speed;
                    if(body.vy==0&&Math.Abs(body.vx)<=settings.sleepSpeed){body.vx=0;body.asleep=true;}
                }
                if(body.Top>settings.ceiling){body.y=settings.ceiling-body.halfHeight;body.vy=Math.Min(0,body.vy);}
                // The table line is an unconditional final constraint, including large time steps.
                if(body.Bottom<settings.surface){body.y=settings.surface+body.halfHeight;body.vy=Math.Max(0,body.vy);}
            }
            return work;
        }
    }
}
