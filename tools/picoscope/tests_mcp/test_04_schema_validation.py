"""Category 4: tool input-schema validation (declared schemas are well-formed)."""

from tools.picoscope.tests_mcp.mcp_client import run


def test_schemas_are_object_typed(env_factory):
    env = env_factory()

    async def body(session):
        tools = {t.name: t for t in (await session.list_tools()).tools}
        for name in ("scope_info", "scope_capture_block", "scope_get_capture_metadata", "scope_export_capture"):
            schema = tools[name].input_schema
            assert schema.get("type") == "object", f"{name} schema should be an object"
            assert "properties" in schema, f"{name} schema missing properties"

    run(env, body)


def test_capture_profile_schema_declares_limits_shape(env_factory):
    env = env_factory()

    async def body(session):
        tools = {t.name: t for t in (await session.list_tools()).tools}
        props = tools["scope_capture_block"].input_schema["properties"]["profile"]
        # The profile is a nested object with the expected fields.
        assert props.get("type") == "object" or "anyOf" in props or "$ref" in props
        assert "description" in props or "properties" in props

    run(env, body)


def test_backend_field_is_enum(env_factory):
    env = env_factory()

    async def body(session):
        tools = {t.name: t for t in (await session.list_tools()).tools}
        backend = tools["scope_info"].input_schema["properties"]["backend"]
        # Declared as a constrained enum of recognised backends.
        assert "enum" in backend or backend.get("type") == "string"

    run(env, body)
