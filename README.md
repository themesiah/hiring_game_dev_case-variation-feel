# hiring_game_dev_case-variation-feel

## Thought process

Before even starting changing things, I checked the project and made a list of elements that would be cool to change, improve or create from scratch.

I will try to put that list in order from most important to less important, following the idea that the most important things are the ones that make the player understand what is happening. On the starting version of the game, it is not clear when you take damage, when you are attacking, and depending on what you do it is not even obvious if you are attacking at all. So most important changes will be direct feedback ones, the minimal for each action happening in the game. After that, i will try to put improvements that make them cooler. Note that this is NOT the list of finished improvements, just a first study of what CAN I do to make the game better, and will do whatever i'm able in the expected time.

Also, the order can be pretty subjective. There are some obvious improvements that is difficult to not put the first on the list, but after that, it becomes a lot of my own opinion. The list would be as follows:

- **Attack, damage and death animations for player and enemies**. Right now player only have idle and run, and enemies idle and run (but they are never idle). First and most important task would be to implement the transition between animations to at least know when things are happening. The most time consuming part will probably be the actions happening WHILE animating, and not before the animation. For example, i expect the enemy to damage the hero instantly, and then play the animation. There are lots of ways to do this, but i will probably take the easy route of adding some delays before causing the damage. That's prone to error and i will try to consider all edge cases (like diying while the timer is on).
- It would be great to have something **indicating which enemy you are attacking**. Some outline, small circle on the bottom of the enemy, an arrow... and the player character looking at that enemy.
- **Current life**. Might not be needed on some designs, but a simple lifebar that changes colors when it gets smaller should be good enough to at least put some pressure on the player when receiving damage.
- **Hit confirmations**. Even if an animation shows the sword swinging or the enemy attacking, it is important to know when the actual damage happened. I think at least a VFX, model color change (flash white when dealt damage for example) and maybe damage numbers might be ok. All of them together should be cool, but might not implement the model color change depending on the shader setup (will explain more later).
- Enemies automatically disappear (game object destroyed, probably) when dying. I will make it so they die after a few seconds, so **the death animation can play and be seen** (potential bug point, i will make sure that they are not targeteable when dead!).
- Some **cool effect of the sword** when swinging would be good, even before it hits.
- *update after starting* A small and fast improvement would be adding some dust cloud behind the player when moving. Should be super easy, and moving is literally the only thing that the player can do (besides not doing nothing and auto attacking, of course), so giving it some feedback should be important.
- **Enemy spawn**. They can appear from above, materialize from transparent to fully opaque, or a VFX with a "poof" and a cloud of dust and they appear from inside, or whatever. Might try the last one as it seems cute.
- **Screen shake**! Easy and looks good if done moderately. Will probably add it for when the player receives damage. Other option is adding it to when the enemies receive damage, but not both. I prefer when the player receives damage as it is easy to think of the shake as something bad (some immersion).
- At this point we already have something indicating which enemy you are attacking. A next step would be a subtle **circle indicating the range of the player**. 
- **Enemy health bars**. I won't put a health bar on each enemy at the same time, because it will occuppy all of the screen. A good middle point is showing a health bar for enemies that either got damaged recently or enemies that have less than max life (i've seen games with lots of enemies do this). However, this is pretty low in the list, and i wouldn't mind if enemies didn't have health bars at all.
- **Audio**. If a miracle makes me get here, I would like to add audio. However, this is last in the list for two reasons: first of all, it can take long to implement, because there is no base in the project and choosing and making sure that the sounds are coherent is somewhat difficult. Second, the game is supposed to be for mobile. In mobile audio is not that important as for other platforms (not saying it is not important, just saying it is less!), and a lot of people play without audio. So i don't think it should be a priority.

After checking the list i don't think i will be able to implement all, but i will follow the list order, will change it if i find something in the game more difficult to read than i though, and will do all i can.

I will then log in this readme each feature I added, how i added and how much time it took to implement it.

## Player animations

**Duration**: 14 minutes

**What i did**: Using hero view, which already had a reference to the hero animator, i added hashes for Damage, Attack and Dead parameters. I added a "last hero state" variable on the hero view to check the change between the last and current state (as i saw that the event is called each time anything from the hero changes). So it attacks if the last attacking time is greater, gets damaged if the health is lower, and is set as dead if the hero state is considered dead. "Dead" is a state coming from "Any State" and don't return to any other state.

**Result**: Player moves, attacks, gets damaged and dies correctly. Attack is probably doing damage at the start of the animation, but will fix it later.

**Update**: While doing enemy animations I noticed that the game reset doesn't spawn the player again. So i fixed the animator to transition from dead to idle.

## Enemy animations

**Duration**: 25 minutes

**What i did**: Same logic as with the player. However, as enemies are treated as a container of enemies and didn't have callbacks for each state update, i did a bit of refactor to support that. After the refactor, logic was the same as with the hero: save the last state, compare states, change animator depending on the changes.

I also was supposed to improve the death animation making enemies disappear after a time, instead of disappearing instantly, but i found the easy way and did it now. Enemies are destroyed after a second (on the enemy config file can be changed) and they do not participate in enemy loops (they are out of the list and also check if an enemy is dead before updating it).

**Result**: Enemies move, stay, attack and die correctly. Dead enemies stay there for a second before disappearing.

**Update**: Added time to disappear to enemy config scriptable object.

## Better animation timing

**Duration**: 15 minutes

**Previous bugfixing**: Noticed that the hero state change event was not being called when hitting enemies and sometimes it didn't attack when needed, so i fixed it. At this point i noticed that i could have as well put this logic on the update (as the event is being called a lot) but it would be not a performance issue for now.

**What i did**: My first idea was to put a delay between the start of the animation and the actual damage from the player and from the enemies, so the actual damage was done at the impact point. But then I noticed two things:

1. You can not have the damage and attack animation at the same time, but attacking and receiving damage happened at the same time a lot.
2. Attack animations were a little slow, and considering that the damage happens instantly, i thought that they should be faster.

There was another issue with the delay idea. What happens during the delay? If the player moves and is far from the enemy, should Alice damage the enemy anyway? Or maybe she shouldn't be able to move? Those questions could easily be out of the scope for this assignment, so i decided to go an easier route.

What i actually did was increasing the speed of the attacks, removing transition time between idle and attacks (because i noticed that attacks actually started from the idle position) and muting the damage animations. This doesn't mean that i won't add damage feedback, it just means that i prefer to add it on some other way that doesn't collide with the attack animations. Because of that, the hit confirmations will most probably be the next improvement i make.

**Result**: Attack animations are now faster and snappier, and will make future feedback easier to implement and appreciate. Damage animations are now muted.