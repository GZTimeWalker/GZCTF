import { useEffect } from 'react'
import { useParams } from 'react-router'
import { Center, Loader, Stack, Text } from '@mantine/core'
import { useNavigate } from 'react-router'

const LearnDeep = () => {
  const { slug, moduleId, itemId } = useParams()
  const navigate = useNavigate()

  useEffect(() => {
    if (!slug || !moduleId || !itemId) {
      navigate('/404', { replace: true })
      return
    }

    const url = `/api/skill-tree-redirects/${encodeURIComponent(slug)}?moduleId=${encodeURIComponent(moduleId)}&itemId=${encodeURIComponent(itemId)}`

    fetch(url)
      .then((res) => {
        if (!res.ok) throw new Error('not found')
        return res.json()
      })
      .then((data: { targetPath: string }) => navigate(data.targetPath, { replace: true }))
      .catch(() => navigate('/404', { replace: true }))
  }, [slug, moduleId, itemId, navigate])

  return (
    <Center h="60vh">
      <Stack align="center">
        <Loader />
        <Text c="dimmed">Loading...</Text>
      </Stack>
    </Center>
  )
}

export default LearnDeep
