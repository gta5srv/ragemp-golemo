namespace GolemoSDK
{
    public class WorkStats
    {
        public int WorkID { get; }
        public int Level { get; private set; }
        public int Exp { get; private set; }
        public int AllPoints { get; private set; }
        public int _maxLevel { get; private set; }
        public int _expCountForUpLevel { get; private set; }
        public WorkStats(int workID, int level, int exp, int allPoints, int maxLevel, int expCountForUpLevel)
        {
            WorkID = workID;
            Level = level;
            Exp = exp;
            AllPoints = allPoints;
            _maxLevel = maxLevel;
            _expCountForUpLevel = expCountForUpLevel;
        }

        public bool AddExp(int value)
        {
            bool isUpLevel = false; //дефолтное состояние
            if (isMaxExp(value))
            {
                isUpLevel = UpLevel();
            }
            else
            {
                this.Exp += value;
            }
            this.AllPoints++;
            return isUpLevel;
        }
        private bool UpLevel()
        {
            if (isMaxLevel())
                return false; //если достигнут максимальный уровень
            this.Level++;
            this.Exp = 1;
            return true;     //если уровень повысился
        }
        private bool isMaxExp(int value)
        {
            if (this.Exp + value > this._expCountForUpLevel)
                return true;
            else
                return false;
        }
        private bool isMaxLevel()
        {
            if (this.Level + 1 > _maxLevel)
                return true;
            else
                return false;
        }
        public void ChangeMaxLevel(int value)
        {
            this._maxLevel = value;
        }
        public void ChangeNeedExp(int value)
        {
            this._expCountForUpLevel = value;
        }
    }
}
