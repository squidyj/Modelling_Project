#version 400

in vec3 position;
in vec3 normal;
in vec3 texcoord;

out vec3 fnorm;
out vec3 fpos;
out vec3 lpos;

uniform mat4 projection;
uniform mat4 modelview;
void main () {

	fpos = position;
	fnorm = normal;
	lpos = vec3 (0, 3, -1);
	gl_Position = projection * modelview * vec4(position, 1);
}