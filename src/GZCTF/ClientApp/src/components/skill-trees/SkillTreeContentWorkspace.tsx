import { Button, Center, Loader, Stack, Text, Title } from '@mantine/core'
import { useMemo } from 'react'
import { Link, Navigate, useParams } from 'react-router'
import { useTranslation } from 'react-i18next'
import { useSkillTree, useMyLearning } from '@Hooks/useSkillTrees'
import { useUser } from '@Hooks/useUser'
import { ChallengeWorkspace } from '@Components/learning/ChallengeWorkspace'
import { LessonWorkspace } from '@Components/learning/LessonWorkspace'
import { WithNavBar } from '@Components/WithNavbar'

const VALID_KINDS = ['challenge', 'lesson'] as const

export const SkillTreeContentWorkspace = () => {
  const { treeId, categoryId, kind, contentId } = useParams()
  const { t } = useTranslation('skillTrees')
  const { user } = useUser()
  const { data: tree, error: treeError } = useSkillTree(treeId)
  const { data: record } = useMyLearning(!!user)

  if (!VALID_KINDS.includes(kind as typeof VALID_KINDS[number])) {
    return <Navigate to="/404" replace />
  }

  if (!user) {
    return (
      <WithNavBar minWidth={0}>
        <Center h="60vh">
          <Stack align="center" gap="md">
            <Title order={3}>Sign in required</Title>
            <Text c="dimmed">Join this skill tree to access its content.</Text>
            <Button component={Link} to="/account/login">
              Sign in
            </Button>
          </Stack>
        </Center>
      </WithNavBar>
    )
  }

  const isEnrolled = record?.skillTrees?.some((item) => item.skillTreeId === treeId) ?? false

  if (!isEnrolled) {
    return (
      <WithNavBar minWidth={0}>
        <Center h="60vh">
          <Stack align="center" gap="md">
            <Title order={3}>Join this tree</Title>
            <Text c="dimmed">You need to join this skill tree before accessing its content.</Text>
            <Button component={Link} to={`/skill-trees/${treeId}`}>
              View tree
            </Button>
          </Stack>
        </Center>
      </WithNavBar>
    )
  }

  if (!tree && !treeError) {
    return (
      <WithNavBar minWidth={0}>
        <Center h="60vh">
          <Loader />
        </Center>
      </WithNavBar>
    )
  }

  if (treeError || !tree) {
    return (
      <WithNavBar minWidth={0}>
        <Center h="60vh">
          <Text c="red">{t('errors.generic')}</Text>
        </Center>
      </WithNavBar>
    )
  }

  const nav = useMemo(() => {
    const categories = tree.categories ?? []
    const flat = categories.flatMap((category) =>
      (category.contents ?? []).map((content) => ({
        ...content,
        categoryId: category.categoryId,
      })),
    )
    const idx = flat.findIndex((item) => item.contentId === contentId)
    const prev = idx > 0 ? flat[idx - 1] : undefined
    const next = idx < flat.length - 1 ? flat[idx + 1] : undefined

    return {
      backHref: `/skill-trees/${treeId}`,
      previousHref: prev
        ? `/skill-trees/${treeId}/${prev.categoryId}/${prev.kind}/${prev.contentId}`
        : undefined,
      nextHref: next
        ? `/skill-trees/${treeId}/${next.categoryId}/${next.kind}/${next.contentId}`
        : undefined,
    }
  }, [tree, treeId, contentId])

  if (kind === 'challenge') {
    return <ChallengeWorkspace challengeId={contentId!} {...nav} />
  }

  return <LessonWorkspace lessonId={contentId!} {...nav} />
}
