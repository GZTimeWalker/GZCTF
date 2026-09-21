import { Group, Stack, Text, Title } from '@mantine/core'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router'
import { skillTreeIcons, type SkillTreeIconKey } from '@Utils/skillTreeAdmin'
import type { SkillTreeDetailResponse, SkillTreeContentSummaryResponse } from '@Api'

type SkillTreeOutlineProps = {
  tree: SkillTreeDetailResponse
}

export const SkillTreeOutline = ({ tree }: SkillTreeOutlineProps) => {
  const { t } = useTranslation('skillTrees')
  const icon = skillTreeIcons[(tree.iconKey as SkillTreeIconKey) ?? 'flag']

  if (!tree.categories || tree.categories.length === 0) {
    return <Text c="dimmed">{t('editor.emptyPreview')}</Text>
  }

  return (
    <Stack gap="xl">
      <Group gap="sm">
        <Text size="xl" aria-hidden>
          {icon}
        </Text>
        <div>
          <Title order={2}>{tree.name}</Title>
          {tree.summary ? <Text c="dimmed">{tree.summary}</Text> : null}
        </div>
      </Group>
      {tree.categories.map((category) => (
        <Stack key={category.categoryId} gap="xs">
          <Title order={3}>{category.name}</Title>
          {category.summary ? <Text c="dimmed">{category.summary}</Text> : null}
          <Stack gap="xs" pl="md">
            {category.contents?.map((content: SkillTreeContentSummaryResponse) => {
              const href = `/skill-trees/${tree.skillTreeId}/${category.categoryId}/${content.kind}/${content.contentId}`
              return (
                <Group key={content.contentId} gap="sm" wrap="nowrap">
                  <Text aria-hidden>
                    {content.kind === 'challenge' ? '🧩' : '📖'}
                  </Text>
                  <Text component={Link} to={href} td="underline">
                    {content.title}
                  </Text>
                  {content.kind === 'challenge' && content.expectedMinutes ? (
                    <Text size="xs" c="dimmed">
                      {content.expectedMinutes} min
                    </Text>
                  ) : null}
                  {content.kind === 'challenge' && content.difficulty ? (
                    <Text size="xs" c="dimmed">
                      {content.difficulty}
                    </Text>
                  ) : null}
                </Group>
              )
            })}
          </Stack>
        </Stack>
      ))}
    </Stack>
  )
}
