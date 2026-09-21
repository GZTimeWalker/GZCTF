import { Badge, Button, Card, Divider, Group, SimpleGrid, Stack, Text, Title } from '@mantine/core'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link, useNavigate } from 'react-router'
import { Role } from '@Api'
import { SkillTreeForm, type SkillTreeFormValues } from '@Components/admin/skill-trees/SkillTreeForm'
import { WithNavBar } from '@Components/WithNavbar'
import { WithRole } from '@Components/WithRole'
import { useAdminSkillTrees, useSkillTreeAdminMutations } from '@Hooks/useSkillTreeAdmin'
import { showSkillTreeError } from '@Utils/skillTreeAdminFeedback'
import { skillTreeIcons } from '@Utils/skillTreeAdmin'

const AdminSkillTrees = () => {
  const { data: trees, isLoading } = useAdminSkillTrees()
  const { t } = useTranslation('skillTrees')
  const { createTree } = useSkillTreeAdminMutations()
  const navigate = useNavigate()
  const [values, setValues] = useState<SkillTreeFormValues>({ name: '', summary: '', iconKey: 'flag' })
  const [nameError, setNameError] = useState<string>()
  const [creating, setCreating] = useState(false)

  const create = async () => {
    const name = values.name.trim()
    if (!name) {
      setNameError(t('form.nameRequired'))
      return
    }

    setNameError(undefined)
    setCreating(true)
    try {
      const result = await createTree({
        name,
        summary: values.summary.trim(),
        iconKey: values.iconKey,
      })
      setValues({ name: '', summary: '', iconKey: 'flag' })
      navigate(`/admin/skill-trees/${result.data.skillTreeId}`)
    } catch (error) {
      showSkillTreeError(error, t)
    } finally {
      setCreating(false)
    }
  }

  return (
    <WithRole requiredRole={Role.Admin}>
      <WithNavBar minWidth={0}>
        <Stack>
          <Group justify="space-between">
            <Title order={1}>{t('list.title')}</Title>
            <Button component={Link} to="/admin/skill-categories" variant="light">
              {t('category.title')}
            </Button>
          </Group>
          {isLoading ? (
            <Text c="dimmed">{t('loading')}</Text>
          ) : trees && trees.length > 0 ? (
            <SimpleGrid cols={{ base: 1, sm: 2, lg: 3 }}>
              {trees.map((tree) => (
                <Card key={tree.skillTreeId} withBorder component={Link} to={`/admin/skill-trees/${tree.skillTreeId}`}>
                  <Group justify="space-between" wrap="nowrap">
                    <Group gap="xs" wrap="nowrap">
                      <Text size="xl" aria-hidden>
                        {skillTreeIcons[(tree.iconKey ?? 'flag') as keyof typeof skillTreeIcons] ?? '🚩'}
                      </Text>
                      <Text fw={600}>{tree.name}</Text>
                    </Group>
                    <Badge color={tree.isPublished ? 'green' : 'gray'} variant="light">
                      {tree.isPublished ? t('list.published') : t('list.draft')}
                    </Badge>
                  </Group>
                  <Text size="sm" c="dimmed" lineClamp={2} mt="xs">
                    {tree.summary}
                  </Text>
                </Card>
              ))}
            </SimpleGrid>
          ) : (
            <Text c="dimmed">{t('list.empty')}</Text>
          )}
          <Divider my="md" label={t('list.createTitle')} labelPosition="left" />
          <SkillTreeForm
            values={values}
            onChange={setValues}
            onSubmit={create}
            submitting={creating}
            nameError={nameError}
            submitLabel={t('list.create')}
          />
        </Stack>
      </WithNavBar>
    </WithRole>
  )
}

export default AdminSkillTrees
