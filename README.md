# hiring_game_dev_case-variation-feel

## External libraries or assets used

I will keep this section first and will be updating with the added libraries or asset packs.

- **DoTween**: For general feedback of things with an start and an end. (Inside folder Plugins/Demigiant)
- [**Free quick effects vol 1**](https://assetstore.unity.com/packages/vfx/particles/free-quick-effects-vol-1-304424): For various effects. I might take effects and slightly modify them to work as we need. (Inside folder GabrielAguiarProductions)
- [**Cartoon FX Remaster free](https://assetstore.unity.com/packages/vfx/particles/cartoon-fx-remaster-free-109565): For various effects. I might take effects and slightly modify them to work as we need. (Inside folder JMO Assets)
- [**Quick Outline**](https://assetstore.unity.com/packages/tools/particles-effects/quick-outline-115488): To show which enemy is being currently targeted. (Inside Enemies folder)

## Bugs and other things to note

- When an enemy spawn, it is sometimes attacked by the player like it was close, even if it spawns at 10 meters from the player.
Not sure why this happens, but you can sometimes see the player doing the attack animation in the air. This happens even with the initial project. (FIXED: It was a race condition that made the enemies start spawning before the container view subscribed to the OnEnemySpawned event, so it didn't instantiate any view. I moved the delay on the loop to the start, so it had an starting delay before spawning the first enemy. Not ideal, it would need a new entry point, but i didn't want to change the architecture right now for this)

## About task times

For each task, I indicated the duration, or how much time did it take to implement. I want you to note that this time does not include the time taken writing this documentation, and some times might look higher if we count choosing assets or deciding between a feature or another.

I write this to be completely fair on the time it took for me to do each thing. The times you see in this document are counted from starting the implementation of the feature to the end of the implementation, after being tested and fixed if needed.

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

## Hit VFX feedback

**Duration**: 15 minutes

**What i did**: First, I selected two "hit" vfx from some free VFX pack i found. I then put the sword hit vfx on the enemies and the enemy hit vfx on the player (because the player is hit by the enemies and the enemies are hit by the sword). I added a reference to the particle systems on the view, and when being hit (already done thanks to the "previous state" trick) I play the particle system. Note that this is a naive approach and i know it. If we expect different hit effects from different enemies or weapons this way of doing things does not work. However, this is fast and gives good results when prototyping.

If needed, I guess enemies should get the weapon from the weapon service and spawn a particle system offered by the weapon. For the player is a little more difficult, because we would need the exact enemy that hit her, and spawn a particle system depending on that. Anyway, I think this is good enough for now.

It took longer than expected because a bug made the player attack in the air and thought this was from my implementation.

**Result**: At the time of receiving damage, player and enemies play a particle system hit effect.

## Damage numbers

**Duration**: 37 minutes

**What i did**: I've seen that there are several types of weapon prepared, even if currently only the greatsword is being used. When I play a game with several equipment pieces, I want to feel powerful when I update to a better weapon. And numbers are one way to show that directly. Looking at other Madbox games, the Idle Zoo Tycoon does that for the money you get from stands or the entrance tickets. This is the same, with a very similar fashion, but in this case the "currency" is the damage you deal. To implement this i made two new classes: DamageText and DamageTextsView.

DamageText controls itself: it gets initialized with a damage number, a color and an action to release it when finished and then gets animated, going up and fading. After fading, it calls the release callback. It gets animated with two tweens using DoTween. DoTween is in safe mode, so if anything happens and the damage text gets destroyed before finishing, there shouldn't be any issue.

DamageTextsView gets the Hero and Enemies services, hears for the "damaged" events of each (events i added doing this task too) and gets a DamageText from an object pool (amount of texts can escalate pretty quickly depending on duration, weapon cooldown, amount of enemies...), puts the DamageText on a point in the screen where the damage was done and initializes it. It initializes the text with the damage number, a color (red if the player was damaged, white if the enemy was damaged) and the pool callback to release the instance.

This had a lot of thought on optimization. My first idea was to put a world space canvas on each entity and put the damage number there, but if there is a lot of enemies, there will be a lot of canvases, and that could be a performance issue. Or at least, doing it with a single canvas didn't forbid me of anything i wanted to do, so it was directly a better solution. I also made a pool for the texts to avoid garbage collection instantiating and destroying them. Cool, right?

**Result**: Whenever an entity takes damage, a small damage number pops up on that entity and gets animated over a second before disappearing.

## Health bar

**Duration**: 11 minutes

**What i did**: This one was pretty easy. On the last task (damage numbers) i used the service locator on my own script for the first time and was still checking out how to make it work. This time i just had to do the same as before (mostly). I made a HealthBarView class that had an slider UI element and a text. At the start of the game, when the hero received damage and when the game gets resetted both are updated.

I chose to make this only for the player for two reasons:

1. Doing it for enemies means having a canvas for each, or managing a slider for each enemy on the same screen space canvas, which is doable but the depth makes it difficult to make it look good.
2. Knowing how much life do you have means a lot for the player. You can plan, you stress out when receive a lot of damage, you have great feedback of how well you are doing. Having it for enemies means knowing how many hits do you need to kill them, and even that is easy without the sliders. Would make a lot more sense for some boss enemy with a lot of life, and in that case the boss life would probably be in the screen space UI, like the player.

**Result**: A green/red slider bar on top of the screen is updated showing less green and more red the more damage you take. A text inside tells you exactly how much health do you have.

## Targeted enemy indicator

**Duration** 39 minutes

**What i did**: If the only thing the player can do is move, they have to know what consecuences their movement have. And the only two consecuences are that you stop attacking (already done with animations) and which enemy is closest to you (so, which one you attack). What i did was highligting the currently closest enemy, or the one the hero is going to attack.
To do that, i added the weapon service reference to the **EnemiesController**, and in the update, I check the distance between each enemy with the player. And the one closest (if closer than the weapon range, that's why we have the service here) gets set as current closest enemy. If it is different than the last closest enemy, we send an event changing the current closest, which is received by **EnemiesContainerView** and enables/disables the outline from the **Quick Outline** package.

**Note**: To avoid the outline and the actual attack to differ on which enemy is being attacked, I changed the "TryFindClosestEnemy" from HeroController to use the found enemy on EnemiesController.

**Result**: A red outline appears on the enemy that is currently being attacked, gets removed if the closest enemy changes or if the enemy dies.