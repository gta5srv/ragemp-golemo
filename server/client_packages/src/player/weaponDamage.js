/* 
	#Пока ещё не полностью добил
	#Если найдете ошибки, или недочеты, вы знаете куда писать
	#канал баги-ошибки
*/
mp.events.add('playerWeaponShot', (targetPosition, targetEntity) => {
	if(targetEntity && targetEntity.type === 'player'){
		targetEntity.setInvincible(true);
	}
});
mp.events.add('outgoingDamage', (sourceEntity, targetEntity, sourcePlayer, weapon, boneIndex, damage) => {
    if(targetEntity.type === 'player'){
		let health = targetEntity.getHealth() * 2;
		let cDamage = damage - (damage * 90/100);
		//targetEntity.setHealth(health - cDamage);
		return true;
	}
});
mp.events.add('incomingDamage', (sourceEntity, sourcePlayer, targetEntity, weapon, boneIndex, damage) => {
   if(targetEntity.type === 'player'){
		targetEntity.setInvincible(true);
		let health = targetEntity.getHealth() * 2;
		let cDamage = damage - (damage * 90/100);
		targetEntity.setHealth(health - cDamage);
		setTimeout(() => {targetEntity.setInvincible(false);}, 100);
		return true;
	}
});