/* 
	#Пока ещё не полностью добил
	#Если найдете ошибки, или недочеты, вы знаете куда писать
	#канал баги-ошибки
	#Система кастомного урона на оружия от JJiGolem#7069

	#Здесь можете найти хеши оружий и т.п.
	https://wiki.rage.mp/index.php?title=Causes_of_death

	#Приватная репозитория на github
	https://github.com/JJiGolem/rage-custom-weapon-damage
*/
//дефолтные проценты, которые мы будем отнимать от входящего урона
let defaultPercent = {"max": 85, "min": 60};
//список оружий и их процент, который мы будем снимать с входящего урона
const weaponDamages = {
	// Пистолеты
	//хеш оружия
	3249783761: {
		//название оружия, это для нас, чтобы в будущем смогли быстро найти нужное нам оружие
		"name": "Heavy Revolver",
		//максимальный процент
		"max": 90,
		//минимальный процент
		"min": 80,
		//эти проценты нужны для функции рандома
	},
	// Пистолет пулеметы
	324215364: {
		"name": "Micro SMG",
		"max": 80,
		"min": 50
	},
	736523883: {
		"name": "SMG",
		"max": 80,
		"min": 50
	},
	171789620: {
		"name": "Combat PDW",
		"max": 60,
		"min": 40
	},
	// Пулеметы
	2144741730: {
		"name": "Combat MG",
		"max": 65,
		"min": 35
	},
	// Карабины
	3220176749: {
		"name": "Assault Rifle",
		"max": 70,
		"min": 45
	},
	// Дробовики
	487013001: {
		"name": "Pump Shotgun",
		"max": 80,
		"min": 30,
	},
	// Снайперы
	100416529: {
		"name": "Sniper Rifle",
		"max": 80,
		"min": 50,
	},
	// Холодное оружие
	3441901897: {
		"name": "Battle Axe",
		"max": 50,
		"min": 40
	}
};
//функция генерации рандомного числа
let randomInt = (min, max) => Math.random() * (max - min) + min;
//Событие принятия входящего попадания игроком
mp.events.add('incomingDamage', (sourceEntity, sourcePlayer, targetEntity, weapon, boneIndex, damage) => {
	if(targetEntity.type === 'player' && sourcePlayer){
		if(global.admingm) return true;
		targetEntity.setInvincible(true);
		let max = defaultPercent.max/100;
		let min = defaultPercent.min/100;
		if(weapon in weaponDamages){
			max = weaponDamages[weapon].max/100;
			min = weaponDamages[weapon].min/100;
		}
		let cDamage = 0;
		let percent = randomInt(min, max);
		//если попадание в голову
		if(boneIndex === 20)
			cDamage = (damage - (damage * percent))/10; //чтобы урон не был слишком большой
		//если в другое любое место
		else 
			cDamage = damage - (damage * percent);
		targetEntity.applyDamageTo(cDamage, true);
		setTimeout(() => {targetEntity.setInvincible(false);}, 25);
		return true;
	}
});
