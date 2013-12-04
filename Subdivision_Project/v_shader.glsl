#version 400

in vec3 position;
in vec3 normal;
in vec2 texcoord;

out vec3 fnorm;
out vec3 fpos;
out vec3 lpos;
out vec2 tex;

uniform mat4 projection;
uniform mat4 modelview;
void main () {

	vec4 ipos = modelview * vec4(position, 1); 
	fnorm = (modelview * vec4(normal, 0)).xyz;
	fpos = ipos.xyz;
	tex = texcoord;
	lpos = vec3 (0, 5, -5);
	lpos = (modelview * vec4(lpos, 1)).xyz;
	gl_Position = projection * ipos;
}