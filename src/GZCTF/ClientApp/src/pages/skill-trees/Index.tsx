import { Button, Group, Paper, SimpleGrid, Skeleton, Stack, Text, Title } from '@mantine/core'
import { useTranslation } from 'react-i18next'
import { useSkillTrees } from '@Hooks/useSkillTrees'
import { SkillTreeCard } from '@Components/skill-trees/SkillTreeCard'
import { WithNavBar } from '@Components/WithNavbar'
import { usePageTitle } from '@Hooks/usePageTitle'
import { useLanguage } from '@Utils/I18n'

const SkillTreeIndex = () => {
  const { t } = useTranslation('skillTrees')
  const { locale } = useLanguage()
  const { data: trees, error, mutate } = useSkillTrees()
  usePageTitle(t('list.title'))

  return (
    <WithNavBar minWidth={0}>
      <Stack gap="xl">
        <Stack gap={4}>
          <Title order={1}>{t('list.title')}</Title>
          <Text c="dimmed">{t('list.empty')}</Text>
        </Stack>
        {error ? (
          <Group>
            <Text c="red">{t('errors.generic')}</Text>
            <Button onClick={() => mutate()}>{t('editor.reload')}</Button>
          </Group>
        ) : !trees ? (
          <SimpleGrid cols={{ base: 1, sm: 2, lg: 3 }}>
            {Array.from({ length: 6 }).map((_, index) => (
              <Paper key={index} withBorder p="md" h={160}>
                <Stack gap="xs">
                  <Group gap="sm">
                    <Skeleton height={32} width={32} />
                    <Skeleton height={24} width="70%" />
                  </Group>
                  <Skeleton height={14} width="90%" />
                  <Skeleton height={14} width={120} mt="auto" />
                </Stack>
              </Paper>
            ))}
          </SimpleGrid>
        ) : trees.length === 0 ? (
          <Text c="dimmed">{t('list.empty')}</Text>
        ) : (
          <SimpleGrid cols={{ base: 1, sm: 2, lg: 3 }}>
            {trees.map((tree) => (
              <SkillTreeCard key={tree.skillTreeId} tree={tree} />
            ))}
          </SimpleGrid>
        )}
      </Stack>
    </WithNavBar>
  )
}

export default SkillTreeIndex
