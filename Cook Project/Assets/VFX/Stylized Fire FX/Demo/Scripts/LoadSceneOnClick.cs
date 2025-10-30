using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

namespace StylizedFireFX
{

public class LoadSceneOnClick : MonoBehaviour
{
	
	public bool GUIHide = false;
	public bool GUIHide2 = false;
	public bool GUIHide3 = false;	
		
    public void CollectionFlamethrower()  {
		SceneManager.LoadScene ("CollectionFlamethrower");
	}
    public void CollectionFull()  {
        SceneManager.LoadScene ("CollectionFull");
	}
    public void CollectionMissiles()  {
        SceneManager.LoadScene ("CollectionMissiles");
	}
    public void CollectionSmall()  {
		SceneManager.LoadScene ("CollectionSmall");
	}
    public void FX1Fire()  {
        SceneManager.LoadScene ("FX1Fire");
	}
    public void FX1FireFull()  {
        SceneManager.LoadScene ("FX1FireFull");
	}
    public void FX1Flamethrower()  {
        SceneManager.LoadScene ("FX1Flamethrower");
	}
    public void FX1Missiles()  {
        SceneManager.LoadScene ("FX1Missiles");
    }
    public void FX2Fire()  {
        SceneManager.LoadScene ("FX2Fire");
    }
    public void FX2FireFull() {
        SceneManager.LoadScene("FX2FireFull");
    }
    public void FX2Flamethrower() {
        SceneManager.LoadScene("FX2Flamethrower");
    }
    public void FX2Missiles() {
        SceneManager.LoadScene("FX2Missiles");
    }
	
	void Update ()
	 {
 
     if(Input.GetKeyDown(KeyCode.L))
	 {
         GUIHide = !GUIHide;
     
         if (GUIHide)
		 {
             GameObject.Find("CanvasFX1").GetComponent<Canvas> ().enabled = false;
         }
		 else
		 {
             GameObject.Find("CanvasFX1").GetComponent<Canvas> ().enabled = true;
         }
     }
	      if(Input.GetKeyDown(KeyCode.J))
	 {
         GUIHide2 = !GUIHide2;
     
         if (GUIHide2)
		 {
             GameObject.Find("CanvasFX2").GetComponent<Canvas> ().enabled = false;
         }
		 else
		 {
             GameObject.Find("CanvasFX2").GetComponent<Canvas> ().enabled = true;
         }
     }
		if(Input.GetKeyDown(KeyCode.H))
	 {
         GUIHide3 = !GUIHide3;
     
         if (GUIHide3)
		 {
             GameObject.Find("CanvasMissileSpawn").GetComponent<Canvas> ().enabled = false;
         }
		 else
		 {
             GameObject.Find("CanvasMissileSpawn").GetComponent<Canvas> ().enabled = true;
         }
     }
	}
}
}