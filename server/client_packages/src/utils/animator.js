function PlayAnim(){mp.events.callRemote("ANIMATOR_EVENT","Animator_PlayAnim",Animations[CurrentAnimation])}let CurrentAnimation=0,Animations=[],AnimatorLaunched=!1;const player=mp.players.local;mp.events.add({StartClientAnimator:()=>{AnimatorLaunched||(AnimatorLaunched=!0)},AddAnimatorAnims:n=>{Animations=Animations.concat(JSON.parse(n))},StopClientAnimator:()=>{Animations=[],AnimatorLaunched=!1},SkipAnimatorData:n=>{n>=Animations.Count||n<0||(CurrentAnimation=n,mp.events.callRemote("ANIMATOR_EVENT","Animator_PlayAnim",Animations[CurrentAnimation]))}});mp.events.add("render",function(){if(AnimatorLaunched&&Animations.length>0){const t=Animations[CurrentAnimation],n=t.split(" ");mp.game.graphics.drawText("Animation: ~n~~g~[ "+CurrentAnimation+" ] ~y~"+n[0].toString()+" ~b~"+n[1].toString(),[.5,.78],{font:4,color:[255,255,255,185],scale:[.5,.5],outline:!0})}});mp.keys.bind(39,!1,function(){AnimatorLaunched&&CurrentAnimation<Animations.length-1&&(CurrentAnimation++,PlayAnim())});mp.keys.bind(37,!1,function(){AnimatorLaunched&&CurrentAnimation>0&&(CurrentAnimation--,PlayAnim())});mp.keys.bind(38,!1,function(){AnimatorLaunched&&CurrentAnimation+100<Animations.length-1&&(CurrentAnimation+=100,PlayAnim())});mp.keys.bind(40,!1,function(){AnimatorLaunched&&CurrentAnimation-100>0&&(CurrentAnimation-=100,PlayAnim())});