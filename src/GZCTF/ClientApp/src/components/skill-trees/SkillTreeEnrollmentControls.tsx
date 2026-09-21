import { ActionIcon, Button, Group, LoadingOverlay, Text } from '@mantine/core'
import { useDisclosure } from '@mantine/hooks'
import { useTranslation } from 'react-i18next'
import { useUser } from '@Hooks/useUser'
import { useMyLearning, useSkillTreeMutations } from '@Hooks/useSkillTrees'

type SkillTreeEnrollmentControlsProps = {
  treeId: string
}

export const SkillTreeEnrollmentControls = ({ treeId }: SkillTreeEnrollmentControlsProps) => {
  const { t } = useTranslation('skillTrees')
  const { user } = useUser()
  const { data: record } = useMyLearning(!!user)
  const { join, leave, setCurrent } = useSkillTreeMutations()
  const [joining, { open: openJoin, close: closeJoin }] = useDisclosure(false)
  const [leaving, { open: openLeave, close: closeLeave }] = useDisclosure(false)
  const [settingCurrent, { open: openSetCurrent, close: closeSetCurrent }] = useDisclosure(false)

  if (!user) {
    return (
      <Button component="a" href="/account/login">
        {t('list.published')}
      </Button>
    )
  }

  const enrollment = record?.skillTrees?.find((item) => item.skillTreeId === treeId)
  const isCurrent = record?.currentSkillTreeId === treeId

  if (!enrollment) {
    return (
      <Button
        loading={joining}
        onClick={async () => {
          openJoin()
          try {
            await join(treeId)
          } finally {
            closeJoin()
          }
        }}
      >
        Join
      </Button>
    )
  }

  if (isCurrent) {
    return (
      <Group gap="xs">
        <Text c="teal" fw={600}>
          Current
        </Text>
        <Button
          color="red"
          variant="light"
          loading={leaving}
          onClick={async () => {
            if (!confirm('Leaving this tree will deselect it. No replacement will be selected automatically.')) return
            openLeave()
            try {
              await leave(treeId)
            } finally {
              closeLeave()
            }
          }}
        >
          Leave
        </Button>
      </Group>
    )
  }

  return (
    <Group gap="xs">
      <Button
        variant="light"
        loading={settingCurrent}
        onClick={async () => {
          openSetCurrent()
          try {
            await setCurrent(treeId)
          } finally {
            closeSetCurrent()
          }
        }}
      >
        Set current
      </Button>
      <Button
        color="red"
        variant="light"
        loading={leaving}
        onClick={async () => {
          openLeave()
          try {
            await leave(treeId)
          } finally {
            closeLeave()
          }
        }}
      >
        Leave
      </Button>
    </Group>
  )
}
