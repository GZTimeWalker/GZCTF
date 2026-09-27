import { Anchor, Group, Paper, Stack, Text, Title } from '@mantine/core'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router'
import { skillTreeIcons, type SkillTreeIconKey } from '@Utils/skillTreeAdmin'
import type { SkillTreeSummaryResponse } from '@Api'

type SkillTreeCardProps = {
  tree: SkillTreeSummaryResponse
}

export const SkillTreeCard = ({ tree }: SkillTreeCardProps) => {
  const { t } = useTranslation('skillTrees')
  const icon = skillTreeIcons[(tree.iconKey as SkillTreeIconKey) ?? 'flag']

  return (
    <Paper withBorder p="md" h="100%">
      <Stack gap="xs">
        <Group gap="sm" wrap="nowrap">
          <Text size="xl" aria-hidden>
            {icon}
          </Text>
          <Title order={3}>
            <Anchor component={Link} to={`/skill-trees/${tree.skillTreeId}`} inherit>
              {tree.name}
            </Anchor>
          </Title>
        </Group>
        {tree.summary ? (
          <Text c="dimmed" lineClamp={2}>
            {tree.summary}
          </Text>
        ) : null}
        <Group gap="md" mt="xs">
          <Text size="sm">{t('list.categoryCount', { count: tree.categoryCount ?? 0 })}</Text>
          <Text size="sm">{t('list.challengeCount', { count: tree.challengeCount ?? 0 })}</Text>
          <Text size="sm">{t('list.lessonCount', { count: tree.lessonCount ?? 0 })}</Text>
        </Group>
      </Stack>
    </Paper>
  )
}
