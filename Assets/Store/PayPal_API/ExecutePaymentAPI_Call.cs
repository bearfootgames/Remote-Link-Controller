using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExecutePaymentAPI_Call : MonoBehaviour {

	public string paymentID;

	public string payerID;

	public string accessToken;

	//[HideInInspector]
	public PayPalExecutePaymentJsonResponse API_SuccessResponse;

	//[HideInInspector]
	public PayPalErrorJsonResponse API_ErrorResponse;

	// Use this for initialization
	void Start () {
		Debug.Log("calling coroutine");
		StartCoroutine (MakePayAPIcall ());
	}

	void handleSuccessResponse(string responseText) {

		//attempt to parse reponse text
		API_SuccessResponse = JsonUtility.FromJson<PayPalExecutePaymentJsonResponse>(responseText);
		
		StartCoroutine(handle_SuccessResponsestring(responseText));
	}
	IEnumerator handle_SuccessResponsestring (string responseText)
	{
		string[] replacements = new string[]{"\"","}","{","[","]"};
		foreach(string r in replacements){
			responseText = responseText.Replace(r,"");
		}

		int qty = 0;
		string id = "";
		Debug.Log(responseText);
		
		string[] stringModifiers = new string[]{"items:name:"};
		string[] itemArr = responseText.Split(stringModifiers,System.StringSplitOptions.None);
		string[] itemList = itemArr[1].Split(","[0]);
		if(itemList[0] == "1 Authorization" || itemList[0] == "1授权"){qty = 1;}
		if(itemList[0] == "2 Authorizations" || itemList[0] == "2授权"){qty = 2;}
		if(itemList[0] == "3 Authorizations" || itemList[0] == "3个授权"){qty = 3;}
		
		stringModifiers = new string[]{"id:"};
		itemArr = responseText.Split(stringModifiers,System.StringSplitOptions.None);
		itemList = itemArr[1].Split(","[0]);
		id = itemList[0];
		
		if(qty>0){
			int L = GameObject.Find("TelloController").GetComponent<TelloController>().language;
			Debug.Log("lang: "+L);
			if(L==0){
				Auth auth = GameObject.Find("Canvas-Auth-English").GetComponent<Auth>();
				CoroutineWithData cd = new CoroutineWithData(this, auth.Account_PHP_receipt(id,qty) );
        		yield return cd.coroutine;
				auth.Purchase_Auth(id,qty);
			}
			if(L==1){
				Auth auth = GameObject.Find("Canvas-Auth-China").GetComponent<Auth>();
				CoroutineWithData cd = new CoroutineWithData(this, auth.Account_PHP_receipt(id,qty) );
        		yield return cd.coroutine;
				auth.Purchase_Auth(id,qty);
			}
		}
	}

	void handleErrorResponse(string responseText, string errorText) {

		//attempt to parse error response 
		API_ErrorResponse = JsonUtility.FromJson<PayPalErrorJsonResponse>(responseText);
		Debug.Log ("parsed response");

	}

	IEnumerator MakePayAPIcall() {

		Dictionary<string,string> headers = new Dictionary<string, string >();

		headers.Add("Content-Type","application/json");
		headers.Add("Authorization","Bearer " + accessToken);

		PayPalExecutePaymentJsonRequest request = new PayPalExecutePaymentJsonRequest ();
		request.payer_id = payerID;

		Debug.Log ("json: " + JsonUtility.ToJson (request));

		byte[] formData = System.Text.Encoding.UTF8.GetBytes(JsonUtility.ToJson(request));

		string baseEndpointURL = StoreProperties.INSTANCE.isUsingSandbox () ?
			"https://api.sandbox.paypal.com/v1/payments/payment/" :
			"https://api.paypal.com/v1/payments/payment/";

		string endpointURL = baseEndpointURL + paymentID + "/execute";
		WWW www = new WWW(endpointURL, formData, headers);

		Debug.Log("Making call to: " + endpointURL);

		yield return www;

		//if ok response
		if (www.error == null) {
			Debug.Log("Execute PaymentAPI_CAll WWW Ok! Full Text: " + www.text);

			handleSuccessResponse (www.text);

		} else {
			Debug.Log("WWW Error: "+ www.error);
			Debug.Log("WWW Text: "+ www.text);

			handleErrorResponse (www.text, www.error);
		}    
	}
}
