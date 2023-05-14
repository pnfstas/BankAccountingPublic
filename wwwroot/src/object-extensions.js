class ObjectExtensions
{
	static hasBasePrototype(object, typeName)
	{
		const prototype = Object.getPrototypeOf(object);
		return prototype != null && (prototype.constructor?.name === typeName || ObjectExtensions.hasBasePrototype(prototype, typeName));
	}
	static isNotEmptyString(object)
	{
		return typeof object === "string" && object.trim().length > 0;
	}
}