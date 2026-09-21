import { Badge, Button, Card, Divider, Group, SimpleGrid, Stack, Text, Title } from '@mantine/core'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link, useNavigate } from 'react-router'
import { Role } from '@Api'
import { SkillTreeForm, type SkillTreeFormValues } from '@Components/admin/skill-trees/SkillTreeForm'
import { WithNavBar } from '@Components/WithNavbar'
import { WithRole } from '@Components/WithRole'
import { useAdminSkillCategories, useSkillTreeAdminMutations } from '@Hooks/useSkillTreeAdmin'
import { skillTreeIcons, type SkillTreeIconKey } from '@Utils/skillTreeAdmin'
import { showSkillTreeError } from '@Utils/skillTreeAdminFeedback'

const AdminSkillCategories = () => {
  const { data: categories, isLoading } = useAdminSkillCategories()
  const { t } = useTranslation('skillTrees')
  const { createCategory } = useSkillTreeAdminMutations()
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
      const result = await createCategory({ name, summary: values.summary.trim(), iconKey: values.iconKey })
      setValues({ name: '', summary: '', iconKey: 'flag' })
      navigate(`/admin/skill-categories/${result.data.categoryId}`)
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
            <Title order={1}>{t('category.title')}</Title>
            <Button component={Link} to="/admin/skill-trees" variant="light">
              {t('list.title')}
            </Button>
          </Group>
          {isLoading ? (
            <Text c="dimmed">{t('loading')}</Text>
          ) : categories && categories.length > 0 ? (
            <SimpleGrid cols={{ base: 1, sm: 2, lg: 3 }}>
              {categories.map((category) => {
                const challenges = (category.contents ?? []).filter((item) => item.kind === 'challenge').length
                const lessons = (category.contents ?? []).filter((item) => item.kind === 'lesson').length
                const treeCount = category.trees?.length ?? 0
                return (
                  <Card
                    key={category.categoryId}
                    withBorder
                    component={Link}
                    to={`/admin/skill-categories/${category.categoryId}`}
                  >
                    <Group justify="space-between" wrap="nowrap">
                      <Group gap="xs" wrap="nowrap">
                        <Text size="xl" aria-hidden>
                          {skillTreeIcons[(category.iconKey ?? 'flag') as SkillTreeIconKey] ?? '🚩'}
                        </Text>
                        <Text fw={600}>{category.name}</Text>
                      </Group>
                      {treeCount === 0 && (
                        <Badge color="orange" variant="light">
                          {t('category.orphan')}
                        </Badge>
                      )}
                    </Group>
                    <Text size="sm" c="dimmed" lineClamp={2} mt="xs">
                      {category.summary}
                    </Text>
                    <Group gap="xs" mt="xs">
                      <Badge variant="outline">{t('category.treeCount', { count: treeCount })}</Badge>
                      <Badge variant="outline">{t('category.challengeCount', { count: challenges })}</Badge>
                      <Badge variant="outline">{t('category.lessonCount', { count: lessons })}</Badge>
                    </Group>
                  </Card>
                )
              })}
            </SimpleGrid>
          ) : (
            <Text c="dimmed">{t('category.empty')}</Text>
          )}
          <Divider my="md" label={t('category.createTitle')} labelPosition="left" />
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

export default AdminSkillCategories
