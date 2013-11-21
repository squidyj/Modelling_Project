#version 400
in vec3 fpos;
in vec3 fnorm;
in vec3 lpos;
void main () {

	vec3 normal = normalize(fnorm);

	vec3 lvec = normalize(lpos - fpos);
	float dif = dot(fnorm, lvec);
	dif = max(dif, 0.0);
	vec3 kd = vec3(0.5, 0.6, 0.4);
	vec3 ld = vec3(1, 1, 1);
	vec3 color = ld * kd * dif;
	gl_FragColor = vec4 (color, 1.0);
}