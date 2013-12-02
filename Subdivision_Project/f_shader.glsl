#version 400
in vec3 fpos;
in vec3 fnorm;
in vec3 lpos;
void main () {

	float exponent = 20.4;
	vec3 normal = normalize(fnorm);
	vec3 lvec = normalize(lpos - fpos);
	vec3 evec = normalize(-fpos);
	vec3 hlf = normalize(evec + lvec);
	float dif = clamp(dot(fnorm, lvec), 0, 1);
	float spec = pow(clamp(dot(fnorm, hlf), 0, 1), exponent);
	vec3 kd = vec3(0.5, 0.6, 0.4);
	vec3 ks = vec3(0.5, 0.5, 0.5);
	vec3 ld = vec3(1, 0.6, 0.6);
	vec3 color = ld * kd * dif + ld * ks * spec;
	gl_FragColor = vec4 (color, 1.0);
}