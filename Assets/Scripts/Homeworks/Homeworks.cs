using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Homeworks
{
    public class Homeworks : MonoBehaviour
    {
        internal static List<Homework> homeworks;
        public void OnEnable()
        {
            if (homeworks == null) StartCoroutine(Manager.provider.GetHomeworks(Initialise));
            else Manager.OpenModule(gameObject);
        }
        public void Initialise(IEnumerable<Homework> _homeworks)
        {
            homeworks = _homeworks.ToList();
            Manager.OpenModule(gameObject);
        }
    }
}
