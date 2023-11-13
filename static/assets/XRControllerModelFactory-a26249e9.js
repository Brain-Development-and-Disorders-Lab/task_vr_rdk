import{O as m,S as f,M as v,a as x}from"./index-47a14fa3.js";import{G as N}from"./GLTFLoader-ead0ad6e.js";const l={Handedness:Object.freeze({NONE:"none",LEFT:"left",RIGHT:"right"}),ComponentState:Object.freeze({DEFAULT:"default",TOUCHED:"touched",PRESSED:"pressed"}),ComponentProperty:Object.freeze({BUTTON:"button",X_AXIS:"xAxis",Y_AXIS:"yAxis",STATE:"state"}),ComponentType:Object.freeze({TRIGGER:"trigger",SQUEEZE:"squeeze",TOUCHPAD:"touchpad",THUMBSTICK:"thumbstick",BUTTON:"button"}),ButtonTouchThreshold:.05,AxisTouchThreshold:.1,VisualResponseProperty:Object.freeze({TRANSFORM:"transform",VISIBILITY:"visibility"})};async function p(s){const e=await fetch(s);if(e.ok)return e.json();throw new Error(e.statusText)}async function y(s){if(!s)throw new Error("No basePath supplied");return await p(`${s}/profilesList.json`)}async function A(s,e,t=null,n=!0){if(!s)throw new Error("No xrInputSource supplied");if(!e)throw new Error("No basePath supplied");const a=await y(e);let r;if(s.profiles.some(i=>{const u=a[i];return u&&(r={profileId:i,profilePath:`${e}/${u.path}`,deprecated:!!u.deprecated}),!!r}),!r){if(!t)throw new Error("No matching profile name found");const i=a[t];if(!i)throw new Error(`No matching profile name found and default profile "${t}" missing.`);r={profileId:t,profilePath:`${e}/${i.path}`,deprecated:!!i.deprecated}}const o=await p(r.profilePath);let h;if(n){let i;if(s.handedness==="any"?i=o.layouts[Object.keys(o.layouts)[0]]:i=o.layouts[s.handedness],!i)throw new Error(`No matching handedness, ${s.handedness}, in profile ${r.profileId}`);i.assetPath&&(h=r.profilePath.replace("profile.json",i.assetPath))}return{profile:o,assetPath:h}}const C={xAxis:0,yAxis:0,button:0,state:l.ComponentState.DEFAULT};function T(s=0,e=0){let t=s,n=e;if(Math.sqrt(s*s+e*e)>1){const o=Math.atan2(e,s);t=Math.cos(o),n=Math.sin(o)}return{normalizedXAxis:t*.5+.5,normalizedYAxis:n*.5+.5}}class E{constructor(e){this.componentProperty=e.componentProperty,this.states=e.states,this.valueNodeName=e.valueNodeName,this.valueNodeProperty=e.valueNodeProperty,this.valueNodeProperty===l.VisualResponseProperty.TRANSFORM&&(this.minNodeName=e.minNodeName,this.maxNodeName=e.maxNodeName),this.value=0,this.updateFromComponent(C)}updateFromComponent({xAxis:e,yAxis:t,button:n,state:a}){const{normalizedXAxis:r,normalizedYAxis:o}=T(e,t);switch(this.componentProperty){case l.ComponentProperty.X_AXIS:this.value=this.states.includes(a)?r:.5;break;case l.ComponentProperty.Y_AXIS:this.value=this.states.includes(a)?o:.5;break;case l.ComponentProperty.BUTTON:this.value=this.states.includes(a)?n:0;break;case l.ComponentProperty.STATE:this.valueNodeProperty===l.VisualResponseProperty.VISIBILITY?this.value=this.states.includes(a):this.value=this.states.includes(a)?1:0;break;default:throw new Error(`Unexpected visualResponse componentProperty ${this.componentProperty}`)}}}class g{constructor(e,t){if(!e||!t||!t.visualResponses||!t.gamepadIndices||Object.keys(t.gamepadIndices).length===0)throw new Error("Invalid arguments supplied");this.id=e,this.type=t.type,this.rootNodeName=t.rootNodeName,this.touchPointNodeName=t.touchPointNodeName,this.visualResponses={},Object.keys(t.visualResponses).forEach(n=>{const a=new E(t.visualResponses[n]);this.visualResponses[n]=a}),this.gamepadIndices=Object.assign({},t.gamepadIndices),this.values={state:l.ComponentState.DEFAULT,button:this.gamepadIndices.button!==void 0?0:void 0,xAxis:this.gamepadIndices.xAxis!==void 0?0:void 0,yAxis:this.gamepadIndices.yAxis!==void 0?0:void 0}}get data(){return{id:this.id,...this.values}}updateFromGamepad(e){if(this.values.state=l.ComponentState.DEFAULT,this.gamepadIndices.button!==void 0&&e.buttons.length>this.gamepadIndices.button){const t=e.buttons[this.gamepadIndices.button];this.values.button=t.value,this.values.button=this.values.button<0?0:this.values.button,this.values.button=this.values.button>1?1:this.values.button,t.pressed||this.values.button===1?this.values.state=l.ComponentState.PRESSED:(t.touched||this.values.button>l.ButtonTouchThreshold)&&(this.values.state=l.ComponentState.TOUCHED)}this.gamepadIndices.xAxis!==void 0&&e.axes.length>this.gamepadIndices.xAxis&&(this.values.xAxis=e.axes[this.gamepadIndices.xAxis],this.values.xAxis=this.values.xAxis<-1?-1:this.values.xAxis,this.values.xAxis=this.values.xAxis>1?1:this.values.xAxis,this.values.state===l.ComponentState.DEFAULT&&Math.abs(this.values.xAxis)>l.AxisTouchThreshold&&(this.values.state=l.ComponentState.TOUCHED)),this.gamepadIndices.yAxis!==void 0&&e.axes.length>this.gamepadIndices.yAxis&&(this.values.yAxis=e.axes[this.gamepadIndices.yAxis],this.values.yAxis=this.values.yAxis<-1?-1:this.values.yAxis,this.values.yAxis=this.values.yAxis>1?1:this.values.yAxis,this.values.state===l.ComponentState.DEFAULT&&Math.abs(this.values.yAxis)>l.AxisTouchThreshold&&(this.values.state=l.ComponentState.TOUCHED)),Object.values(this.visualResponses).forEach(t=>{t.updateFromComponent(this.values)})}}class b{constructor(e,t,n){if(!e)throw new Error("No xrInputSource supplied");if(!t)throw new Error("No profile supplied");this.xrInputSource=e,this.assetUrl=n,this.id=t.profileId,this.layoutDescription=t.layouts[e.handedness],this.components={},Object.keys(this.layoutDescription.components).forEach(a=>{const r=this.layoutDescription.components[a];this.components[a]=new g(a,r)}),this.updateFromGamepad()}get gripSpace(){return this.xrInputSource.gripSpace}get targetRaySpace(){return this.xrInputSource.targetRaySpace}get data(){const e=[];return Object.values(this.components).forEach(t=>{e.push(t.data)}),e}updateFromGamepad(){Object.values(this.components).forEach(e=>{e.updateFromGamepad(this.xrInputSource.gamepad)})}}const w="https://cdn.jsdelivr.net/npm/@webxr-input-profiles/assets@1.0/dist/profiles",P="generic-trigger";class O extends m{constructor(){super(),this.motionController=null,this.envMap=null}setEnvironmentMap(e){return this.envMap==e?this:(this.envMap=e,this.traverse(t=>{t.isMesh&&(t.material.envMap=this.envMap,t.material.needsUpdate=!0)}),this)}updateMatrixWorld(e){super.updateMatrixWorld(e),this.motionController&&(this.motionController.updateFromGamepad(),Object.values(this.motionController.components).forEach(t=>{Object.values(t.visualResponses).forEach(n=>{const{valueNode:a,minNode:r,maxNode:o,value:h,valueNodeProperty:i}=n;a&&(i===l.VisualResponseProperty.VISIBILITY?a.visible=h:i===l.VisualResponseProperty.TRANSFORM&&(a.quaternion.slerpQuaternions(r.quaternion,o.quaternion,h),a.position.lerpVectors(r.position,o.position,h)))})}))}}function I(s,e){Object.values(s.components).forEach(t=>{const{type:n,touchPointNodeName:a,visualResponses:r}=t;if(n===l.ComponentType.TOUCHPAD)if(t.touchPointNode=e.getObjectByName(a),t.touchPointNode){const o=new f(.001),h=new v({color:255}),i=new x(o,h);t.touchPointNode.add(i)}else console.warn(`Could not find touch dot, ${t.touchPointNodeName}, in touchpad component ${t.id}`);Object.values(r).forEach(o=>{const{valueNodeName:h,minNodeName:i,maxNodeName:u,valueNodeProperty:c}=o;if(c===l.VisualResponseProperty.TRANSFORM){if(o.minNode=e.getObjectByName(i),o.maxNode=e.getObjectByName(u),!o.minNode){console.warn(`Could not find ${i} in the model`);return}if(!o.maxNode){console.warn(`Could not find ${u} in the model`);return}}o.valueNode=e.getObjectByName(h),o.valueNode||console.warn(`Could not find ${h} in the model`)})})}function d(s,e){I(s.motionController,e),s.envMap&&e.traverse(t=>{t.isMesh&&(t.material.envMap=s.envMap,t.material.needsUpdate=!0)}),s.add(e)}class F{constructor(e=null){this.gltfLoader=e,this.path=w,this._assetCache={},this.gltfLoader||(this.gltfLoader=new N)}createControllerModel(e){const t=new O;let n=null;return e.addEventListener("connected",a=>{const r=a.data;r.targetRayMode!=="tracked-pointer"||!r.gamepad||A(r,this.path,P).then(({profile:o,assetPath:h})=>{t.motionController=new b(r,o,h);const i=this._assetCache[t.motionController.assetUrl];if(i)n=i.scene.clone(),d(t,n);else{if(!this.gltfLoader)throw new Error("GLTFLoader not set.");this.gltfLoader.setPath(""),this.gltfLoader.load(t.motionController.assetUrl,u=>{this._assetCache[t.motionController.assetUrl]=u,n=u.scene.clone(),d(t,n)},null,()=>{throw new Error(`Asset ${t.motionController.assetUrl} missing or malformed.`)})}}).catch(o=>{console.warn(o)})}),e.addEventListener("disconnected",()=>{t.motionController=null,t.remove(n),n=null}),t}}export{F as XRControllerModelFactory};
